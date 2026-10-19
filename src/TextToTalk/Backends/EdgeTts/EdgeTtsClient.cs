using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TextToTalk.Backends.EdgeTts;

/// <summary>
/// 微软 Edge「大声朗读」语音服务客户端。无需 API Key，走和浏览器朗读相同的公开接口。
/// 参考实现：rany2/edge-tts（MIT）。
/// </summary>
public sealed class EdgeTtsClient : IDisposable
{
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string ChromiumVersion = "143.0.3650.75";
    private const string SecMsGecVersion = "1-" + ChromiumVersion;

    // 2026 年 10 月起旧域名对合成请求返回 403，改用新的 msedgeservices 端点。
    private const string WsEndpoint =
        "wss://api.msedgeservices.com/tts/cognitiveservices/websocket/v1";

    private const string VoicesEndpoint =
        "https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36 Edg/143.0.0.0";

    private readonly HttpClient http;
    private readonly List<EdgeTtsVoice> voices = new();
    private readonly object voicesLock = new();

    public EdgeTtsClient(HttpClient http)
    {
        this.http = http;
    }

    public bool VoicesLoaded { get; private set; }
    public string? LastError { get; private set; }

    public IReadOnlyList<EdgeTtsVoice> Voices
    {
        get
        {
            lock (this.voicesLock)
            {
                return this.voices.ToArray();
            }
        }
    }

    /// <summary>加载语音列表，失败时回退到内置列表。</summary>
    public async Task LoadVoicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{VoicesEndpoint}?trustedclienttoken={TrustedClientToken}" +
                      $"&Sec-MS-GEC={GenerateSecMsGec()}&Sec-MS-GEC-Version={ChromiumVersion}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");

            using var response = await this.http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var list = JsonSerializer.Deserialize<List<EdgeTtsVoice>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (list is { Count: > 0 })
            {
                lock (this.voicesLock)
                {
                    this.voices.Clear();
                    this.voices.AddRange(list.Where(v => !string.IsNullOrWhiteSpace(v.ShortName)));
                }

                VoicesLoaded = true;
                LastError = null;
                return;
            }
        }
        catch (Exception e)
        {
            LastError = e.Message;
            DetailedLog.Warn("Failed to fetch Edge TTS voice list: {Error}", e.Message);
        }

        lock (this.voicesLock)
        {
            if (this.voices.Count == 0)
            {
                this.voices.AddRange(GetFallbackVoices());
            }
        }

        VoicesLoaded = true;
    }

    /// <summary>合成语音，返回 MP3 数据流；失败时返回空流。</summary>
    public async Task<MemoryStream> SynthesizeAsync(string text, string voiceShortName, int ratePercent,
        CancellationToken cancellationToken = default)
    {
        var audio = new MemoryStream();
        if (string.IsNullOrWhiteSpace(text)) return audio;

        var connectionId = Guid.NewGuid().ToString("N");
        var url = $"{WsEndpoint}?ConnectionId={connectionId}" +
                  $"&Ocp-Apim-Subscription-Key={TrustedClientToken}" +
                  $"&Sec-MS-GEC={GenerateSecMsGec()}&Sec-MS-GEC-Version={SecMsGecVersion}";

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
        ws.Options.SetRequestHeader("User-Agent", UserAgent);
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");

        await ws.ConnectAsync(new Uri(url), cancellationToken);

        var timestamp = DateTimeOffset.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'",
            CultureInfo.InvariantCulture);

        // 1. 语音配置
        const string configPayload =
            "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
        await SendTextAsync(ws,
            $"X-Timestamp:{timestamp}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n{configPayload}",
            cancellationToken);

        // 2. SSML
        var lang = GetLocale(voiceShortName);
        var rate = $"{(ratePercent >= 0 ? "+" : string.Empty)}{ratePercent}%";
        var ssml =
            $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{lang}'>" +
            $"<voice name='{Escape(voiceShortName)}'><prosody pitch='+0Hz' rate='{rate}' volume='+0%'>" +
            $"{Escape(text)}</prosody></voice></speak>";

        await SendTextAsync(ws,
            $"X-RequestId:{Guid.NewGuid():N}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{timestamp}\r\nPath:ssml\r\n\r\n{ssml}",
            cancellationToken);

        // 3. 收音频
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();

        while (ws.State == WebSocketState.Open)
        {
            message.SetLength(0);
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close) break;

            var data = message.ToArray();
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var textMessage = Encoding.UTF8.GetString(data);
                if (textMessage.Contains("Path:turn.end")) break;
                continue;
            }

            // 二进制帧：2 字节大端头长度 + 头 + 音频
            if (data.Length < 2) continue;
            var headerLength = (data[0] << 8) | data[1];
            var audioStart = 2 + headerLength;
            if (data.Length <= audioStart) continue;

            audio.Write(data, audioStart, data.Length - audioStart);
        }

        if (ws.State == WebSocketState.Open)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            catch
            {
                // 忽略关闭异常
            }
        }

        audio.Position = 0;
        return audio;
    }

    private static async Task SendTextAsync(ClientWebSocket ws, string payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static string GetLocale(string shortName)
    {
        var parts = shortName.Split('-');
        return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : "en-US";
    }

    private static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");

    /// <summary>Edge 服务要求的 Sec-MS-GEC 校验串。</summary>
    private static string GenerateSecMsGec()
    {
        // Windows 文件时间（100ns），按 5 分钟取整，并加 5 分钟容忍时钟偏差
        var ticks = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 11644473600L + 300;
        ticks -= ticks % 300;
        ticks *= 10_000_000L;

        var raw = $"{ticks}{TrustedClientToken}";
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static IEnumerable<EdgeTtsVoice> GetFallbackVoices()
    {
        var entries = new (string ShortName, string Locale, string Gender, string Name)[]
        {
            ("zh-CN-XiaoxiaoNeural", "zh-CN", "Female", "Xiaoxiao"),
            ("zh-CN-XiaoyiNeural", "zh-CN", "Female", "Xiaoyi"),
            ("zh-CN-YunjianNeural", "zh-CN", "Male", "Yunjian"),
            ("zh-CN-YunxiNeural", "zh-CN", "Male", "Yunxi"),
            ("zh-CN-YunxiaNeural", "zh-CN", "Male", "Yunxia"),
            ("zh-CN-YunyangNeural", "zh-CN", "Male", "Yunyang"),
            ("zh-CN-liaoning-XiaobeiNeural", "zh-CN-liaoning", "Female", "Xiaobei"),
            ("zh-CN-shaanxi-XiaoniNeural", "zh-CN-shaanxi", "Female", "Xiaoni"),
            ("zh-HK-HiuGaaiNeural", "zh-HK", "Female", "HiuGaai"),
            ("zh-HK-HiuMaanNeural", "zh-HK", "Female", "HiuMaan"),
            ("zh-HK-WanLungNeural", "zh-HK", "Male", "WanLung"),
            ("zh-TW-HsiaoChenNeural", "zh-TW", "Female", "HsiaoChen"),
            ("zh-TW-HsiaoYuNeural", "zh-TW", "Female", "HsiaoYu"),
            ("zh-TW-YunJheNeural", "zh-TW", "Male", "YunJhe"),
            ("en-US-AriaNeural", "en-US", "Female", "Aria"),
            ("en-US-GuyNeural", "en-US", "Male", "Guy"),
            ("en-US-JennyNeural", "en-US", "Female", "Jenny"),
            ("en-GB-SoniaNeural", "en-GB", "Female", "Sonia"),
            ("ja-JP-NanamiNeural", "ja-JP", "Female", "Nanami"),
            ("ko-KR-SunHiNeural", "ko-KR", "Female", "SunHi"),
        };

        return entries.Select(e => new EdgeTtsVoice
        {
            ShortName = e.ShortName,
            Locale = e.Locale,
            Gender = e.Gender,
            FriendlyName = $"Microsoft {e.Name} Online (Natural) - {e.Locale}",
        });
    }

    public void Dispose()
    {
    }
}
