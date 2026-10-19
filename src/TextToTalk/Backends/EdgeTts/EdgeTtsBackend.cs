using System;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using TextToTalk.Resources;
using TextToTalk.Services;

namespace TextToTalk.Backends.EdgeTts;

/// <summary>
/// 免密钥的 Edge 朗读后端：直接调用微软 Edge 浏览器的在线语音合成接口。
/// </summary>
public class EdgeTtsBackend : VoiceBackend
{
    private readonly StreamSoundQueue soundQueue;
    private readonly EdgeTtsClient client;
    private readonly EdgeTtsBackendUI ui;
    private readonly INotificationService notificationService;

    public EdgeTtsBackend(PluginConfiguration config, HttpClient http,
        INotificationService notificationService)
    {
        this.notificationService = notificationService;

        TitleBarColor = new Vector4(0.10f, 0.45f, 0.65f, 0.85f);
        this.soundQueue = new StreamSoundQueue(config);
        this.client = new EdgeTtsClient(http);
        this.ui = new EdgeTtsBackendUI(config, this.client, this);
    }

    public override void Say(SayRequest request)
    {
        if (request.Voice is not EdgeTtsVoicePreset preset)
            throw new InvalidOperationException("Invalid voice preset provided.");

        var text = request.Text;
        var voice = string.IsNullOrWhiteSpace(preset.VoiceName) ? "zh-CN-XiaoxiaoNeural" : preset.VoiceName!;
        var rate = preset.Rate;
        var volume = preset.Volume;
        var source = request.Source;

        _ = Task.Run(async () =>
        {
            try
            {
                var data = await this.client.SynthesizeAsync(text, voice, rate);
                if (data.Length == 0)
                {
                    this.notificationService.NotifyWarning(
                        Strings.Get("EdgeTtsEmptyAudio"),
                        Strings.Get("EdgeTtsEmptyAudioHint"));
                    return;
                }

                this.soundQueue.EnqueueSound(data, source, StreamFormat.Mp3, volume);
            }
            catch (Exception e)
            {
                DetailedLog.Error(e, "Failed to synthesize Edge TTS audio.");
                this.notificationService.NotifyError(Strings.Get("EdgeTtsFailed"), e.Message);
            }
        });
    }

    public override void CancelAllSpeech()
    {
        this.soundQueue.CancelAllSounds();
    }

    public override void CancelSay(TextSource source)
    {
        this.soundQueue.CancelFromSource(source);
    }

    public override void DrawSettings(IConfigUIDelegates helpers)
    {
        this.ui.DrawSettings(helpers);
    }

    public override void DrawStyles(IConfigUIDelegates helpers)
    {
    }

    public override TextSource GetCurrentlySpokenTextSource()
    {
        return this.soundQueue.GetCurrentlySpokenTextSource();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.soundQueue.Dispose();
            this.client.Dispose();
        }
    }
}
