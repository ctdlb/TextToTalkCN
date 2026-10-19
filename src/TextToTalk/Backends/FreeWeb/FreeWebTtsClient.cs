using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TextToTalk.Backends.FreeWeb;

/// <summary>
/// 免密钥的网页翻译语音：百度翻译与谷歌翻译的朗读接口。
/// 两者都是公开网页接口，不需要账号，但属于非官方用法，可能随时失效。
/// </summary>
public sealed class FreeWebTtsClient
{
    public const string ProviderBaidu = "Baidu";
    public const string ProviderGoogle = "Google";

    /// <summary>统一语言键 -> 显示名 / 各服务代码。</summary>
    public static readonly IReadOnlyList<FreeWebLanguage> Languages = new List<FreeWebLanguage>
    {
        new("zh", "中文（普通话）", "zh", "zh-CN"),
        new("zh-TW", "中文（台湾）", "zh", "zh-TW"),
        new("en", "英语", "en", "en"),
        new("ja", "日语", "jp", "ja"),
        new("ko", "韩语", "kor", "ko"),
        new("fr", "法语", "fra", "fr"),
        new("es", "西班牙语", "spa", "es"),
        new("de", "德语", "de", "de"),
        new("ru", "俄语", "ru", "ru"),
        new("pt", "葡萄牙语", "pt", "pt"),
        new("it", "意大利语", "it", "it"),
        new("th", "泰语", "th", "th"),
        new("vi", "越南语", "vi", "vi"),
        new("id", "印尼语", "id", "id"),
        new("ar", "阿拉伯语", "ar", "ar"),
    };

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36";

    private readonly HttpClient http;

    public FreeWebTtsClient(HttpClient http)
    {
        this.http = http;
    }

    public static FreeWebLanguage GetLanguage(string key) =>
        Languages.FirstOrDefault(l => l.Key == key) ?? Languages[0];

    public async Task<MemoryStream> SynthesizeAsync(string text, string provider, string languageKey, int speed,
        CancellationToken cancellationToken = default)
    {
        var audio = new MemoryStream();
        if (string.IsNullOrWhiteSpace(text)) return audio;

        var language = GetLanguage(languageKey);
        var maxChunk = provider == ProviderGoogle ? 170 : 500;

        foreach (var chunk in SplitText(text, maxChunk))
        {
            var url = provider == ProviderGoogle
                ? BuildGoogleUrl(chunk, language.GoogleCode)
                : BuildBaiduUrl(chunk, language.BaiduCode, Math.Clamp(speed, 1, 9));

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "audio/mpeg,*/*");
            if (provider == ProviderBaidu)
            {
                request.Headers.TryAddWithoutValidation("Referer", "https://fanyi.baidu.com/");
            }

            using var response = await this.http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await stream.CopyToAsync(audio, cancellationToken);
        }

        audio.Position = 0;
        return audio;
    }

    private static string BuildBaiduUrl(string text, string lan, int speed) =>
        $"https://fanyi.baidu.com/gettts?lan={Uri.EscapeDataString(lan)}&text={Uri.EscapeDataString(text)}" +
        $"&spd={speed}&source=web";

    private static string BuildGoogleUrl(string text, string tl) =>
        $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={Uri.EscapeDataString(tl)}" +
        $"&ttsspeed=1&q={Uri.EscapeDataString(text)}";

    /// <summary>按标点切分长文本，避免超过接口长度上限。</summary>
    private static IEnumerable<string> SplitText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            yield return text;
            yield break;
        }

        var separators = new[] { '\n', '。', '！', '？', '；', '.', '!', '?', ';', '，', ',', ' ' };
        var current = new StringBuilder();

        foreach (var ch in text)
        {
            current.Append(ch);
            if (current.Length >= maxLength)
            {
                var cut = -1;
                for (var i = current.Length - 1; i > current.Length / 2; i--)
                {
                    if (Array.IndexOf(separators, current[i]) >= 0)
                    {
                        cut = i;
                        break;
                    }
                }

                if (cut >= 0)
                {
                    yield return current.ToString(0, cut + 1);
                    var rest = current.ToString(cut + 1, current.Length - cut - 1);
                    current.Clear();
                    current.Append(rest);
                }
                else
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
        }

        if (current.Length > 0) yield return current.ToString();
    }
}

public sealed record FreeWebLanguage(string Key, string DisplayName, string BaiduCode, string GoogleCode);
