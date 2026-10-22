using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TextToTalk.Resources;
using TextToTalk.Services;

namespace TextToTalk.Backends.FishAudio;

public class FishAudioBackend : VoiceBackend
{
    private readonly FishAudioBackendUI ui;
    private readonly FishAudioBackendUIModel uiModel;
    private readonly INotificationService notificationService;
    private readonly PluginConfiguration config;

    public FishAudioBackend(PluginConfiguration config, HttpClient http, INotificationService notificationService)
    {
        this.uiModel = new FishAudioBackendUIModel(config, http);
        this.ui = new FishAudioBackendUI(uiModel, config, this);
        this.notificationService = notificationService;
        this.config = config;
    }

    public override void DrawStyles(IConfigUIDelegates helpers)
    {
        helpers.OpenVoiceStylesConfig();
    }

    public override void Say(SayRequest request)
    {
        if (request.Voice is not FishAudioVoicePreset fishAudioVoicePreset)
        {
            throw new InvalidOperationException("Invalid voice preset provided.");
        }

        var handle = BeginSynthesis(request.Source);
        _ = Task.Run(async () =>
        {
            try
            {
                await this.uiModel.FishAudio.Say(fishAudioVoicePreset.VoiceId, fishAudioVoicePreset.PlaybackRate,
                    fishAudioVoicePreset.Volume, fishAudioVoicePreset.Temperature, fishAudioVoicePreset.TopP,
                    fishAudioVoicePreset.ModelId, fishAudioVoicePreset.Latency, request.Source, request.Text, !string.IsNullOrWhiteSpace(request.Style) ? request.Style : (fishAudioVoicePreset.Style ?? string.Empty),
                    handle.Token);
                if (handle.Token.IsCancellationRequested) return;
                this.uiModel.UpdateApiCreditInfo();
            }
            catch (OperationCanceledException)
            {
                // 用户取消或切场景，属于正常路径。
            }
            catch (FishAudioUnauthorizedException e)
            {
                DetailedLog.Error(e, "Fish Audio API key is incorrect or invalid.");
            }
            catch (FishAudioFailedException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                DetailedLog.Error(e, $"Failed to make Fish Audio TTS request ({e.StatusCode}).");
                this.notificationService.NotifyWarning(Strings.RateLimitWarningTitle,
                    Strings.RateLimitWarningBody);
            }
            catch (FishAudioFailedException e)
            {
                DetailedLog.Error(e, $"Failed to make Fish Audio TTS request ({e.StatusCode}).");
            }
            catch (FishAudioMissingCredentialsException e)
            {
                DetailedLog.Warn(e.Message);
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
        this.uiModel.SoundQueue.CancelAllSounds();
    }

    public override void CancelSay(TextSource source)
    {
        CancelPendingSynthesis(source);
        this.uiModel.SoundQueue.CancelFromSource(source);
    }

    public override void DrawSettings(IConfigUIDelegates helpers)
    {
        this.ui.DrawSettings();
    }

    public override TextSource GetCurrentlySpokenTextSource()
    {
        return this.uiModel.SoundQueue.GetCurrentlySpokenTextSource();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.uiModel.Dispose();
        }
    }
}
