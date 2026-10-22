using System;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using TextToTalk.Resources;
using TextToTalk.Services;

namespace TextToTalk.Backends.FreeWeb;

/// <summary>
/// 免密钥的网页语音后端：百度翻译 / 谷歌翻译的朗读接口。
/// </summary>
public class FreeWebTtsBackend : VoiceBackend
{
    private readonly StreamSoundQueue soundQueue;
    private readonly FreeWebTtsClient client;
    private readonly FreeWebTtsBackendUI ui;
    private readonly INotificationService notificationService;

    public FreeWebTtsBackend(PluginConfiguration config, HttpClient http, INotificationService notificationService)
    {
        this.notificationService = notificationService;

        TitleBarColor = new Vector4(0.20f, 0.55f, 0.35f, 0.85f);
        this.soundQueue = new StreamSoundQueue(config);
        this.client = new FreeWebTtsClient(http);
        this.ui = new FreeWebTtsBackendUI(config, this.client, this);
    }

    public override void Say(SayRequest request)
    {
        if (request.Voice is not FreeWebVoicePreset preset)
            throw new InvalidOperationException("Invalid voice preset provided.");

        var text = request.Text;
        var provider = preset.Provider;
        var language = preset.Language;
        var speed = preset.Speed;
        var volume = preset.Volume;
        var source = request.Source;

        var handle = BeginSynthesis(source);
        _ = Task.Run(async () =>
        {
            try
            {
                var data = await this.client.SynthesizeAsync(text, provider, language, speed, handle.Token)
                    .WaitAsync(handle.Token);
                if (handle.Token.IsCancellationRequested) return;
                if (data.Length == 0)
                {
                    this.notificationService.NotifyWarning(
                        Strings.Get("FreeWebEmptyAudio"),
                        Strings.Get("FreeWebEmptyAudioHint"));
                    return;
                }

                this.soundQueue.EnqueueSound(data, source, StreamFormat.Mp3, volume);
            }
            catch (OperationCanceledException)
            {
                // 用户取消或切场景，属于正常路径。
            }
            catch (Exception e)
            {
                DetailedLog.Error(e, "Failed to synthesize free web TTS audio.");
                this.notificationService.NotifyError(Strings.Get("FreeWebFailed"), e.Message);
            }
            finally
            {
                handle.Dispose();
            }
        });
    }

    public override void CancelAllSpeech()
    {
        CancelPendingSynthesis();
        this.soundQueue.CancelAllSounds();
    }

    public override void CancelSay(TextSource source)
    {
        CancelPendingSynthesis(source);
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
        }
    }
}
