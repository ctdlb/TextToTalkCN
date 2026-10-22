using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TextToTalk.Resources;
using TextToTalk.Services;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsBackend : VoiceBackend
{
    private readonly ElevenLabsBackendUI ui;
    private readonly ElevenLabsBackendUIModel uiModel;
    private readonly INotificationService notificationService;
    private readonly PluginConfiguration config;

    public ElevenLabsBackend(PluginConfiguration config, HttpClient http, INotificationService notificationService)
    {
        this.uiModel = new ElevenLabsBackendUIModel(config, http);
        this.ui = new ElevenLabsBackendUI(uiModel, config, this);
        this.notificationService = notificationService;
        this.config = config;
    }

    public override void DrawStyles(IConfigUIDelegates helpers)
    {
        helpers.OpenVoiceStylesConfig();
    }

    public override void Say(SayRequest request)
    {
        if (request.Voice is not ElevenLabsVoicePreset elevenLabsVoicePreset)
        {
            throw new InvalidOperationException("Invalid voice preset provided.");
        }

        var handle = BeginSynthesis(request.Source);
        _ = Task.Run(async () =>
        {
            try
            {
                await this.uiModel.ElevenLabs.Say(elevenLabsVoicePreset.VoiceId, elevenLabsVoicePreset.PlaybackRate,
                    elevenLabsVoicePreset.Volume, elevenLabsVoicePreset.SimilarityBoost,
                    elevenLabsVoicePreset.Stability, request.Source, request.Text, elevenLabsVoicePreset.ModelId, !string.IsNullOrWhiteSpace(request.Style) ? request.Style : (elevenLabsVoicePreset.Style ?? string.Empty),
                    handle.Token);
                if (handle.Token.IsCancellationRequested) return;
                this.uiModel.UpdateUserSubscriptionInfo();
            }
            catch (OperationCanceledException)
            {
                // 用户取消或切场景，属于正常路径。
            }
            catch (ElevenLabsUnauthorizedException e)
            {
                DetailedLog.Error(e, "ElevenLabs API key is incorrect or invalid.");
            }
            catch (ElevenLabsFailedException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                DetailedLog.Error(e, $"Failed to make ElevenLabs TTS request ({e.StatusCode}).");
                this.notificationService.NotifyWarning(Strings.RateLimitWarningTitle,
                    Strings.RateLimitWarningBody);
            }
            catch (ElevenLabsFailedException e)
            {
                DetailedLog.Error(e, $"Failed to make ElevenLabs TTS request ({e.StatusCode}).");
            }
            catch (ElevenLabsMissingCredentialsException e)
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