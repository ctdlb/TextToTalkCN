using Dalamud.Interface;
using System;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using TextToTalk.Backends.Azure;
using TextToTalk.Backends.EdgeTts;
using TextToTalk.Backends.ElevenLabs;
using TextToTalk.Backends.FishAudio;
using TextToTalk.Backends.FreeWeb;
using TextToTalk.Backends.GoogleCloud;
using TextToTalk.Backends.Kokoro;
using TextToTalk.Backends.Megaphone;
using TextToTalk.Backends.OpenAI;
using TextToTalk.Backends.Polly;
using TextToTalk.Backends.System;
using TextToTalk.Backends.Uberduck;
using TextToTalk.Backends.Websocket;
using TextToTalk.Events;
using TextToTalk.Resources;
using TextToTalk.Services;

namespace TextToTalk.Backends
{
    public class VoiceBackendManager : VoiceBackend
    {
        private readonly HttpClient http;
        private readonly PluginConfiguration config;
        private readonly IUiBuilder uiBuilder;
        private readonly INotificationService notificationService;

        private Task backendTask = Task.CompletedTask;
        private TTSBackend currentKind;
        private bool fallbackAttempted;

        public VoiceBackend? Backend { get; private set; }
        public bool BackendLoading { get; private set; }

        public VoiceBackendManager(PluginConfiguration config, HttpClient http, IUiBuilder uiBuilder,
            INotificationService notificationService)
        {
            this.config = config;
            this.http = http;
            this.uiBuilder = uiBuilder;
            this.notificationService = notificationService;

            this.currentKind = config.Backend;
            SetBackend(this.config.Backend);           
        }

        public override void Say(SayRequest request)
        {
            Backend?.Say(request);
        }

        public override void CancelAllSpeech()
        {
            Backend?.CancelAllSpeech();
        }

        public override void CancelSay(TextSource source)
        {
            Backend?.CancelSay(source);
        }

        public override void DrawSettings(IConfigUIDelegates helpers)
        {
            Backend?.DrawSettings(helpers);
        }
        public override void DrawStyles(IConfigUIDelegates helpers)
        {
            Backend?.DrawStyles(helpers);
        }

        public override TextSource GetCurrentlySpokenTextSource()
        {
            return Backend?.GetCurrentlySpokenTextSource() ?? TextSource.None;
        }

        public override void OnNpcDialogueSessionEvent(NpcDialogueSessionEvent ev)
        {
            Backend?.OnNpcDialogueSessionEvent(ev);
        }

        public void SetBackend(TTSBackend backendKind)
        {
            var kind = backendKind;
            backendTask = backendTask.ContinueWith(_ =>
            {
                BackendLoading = true;
                var oldBackend = Backend;
                try
                {
                    // 先创建新后端，成功后再释放旧的，避免切换失败后完全没有后端
                    var newBackend = CreateBackendFor(kind);
                    newBackend.Start();

                    oldBackend?.Stop();
                    oldBackend?.Dispose();

                    Backend = newBackend;
                    this.currentKind = kind;
                    this.fallbackAttempted = false;
                    WarnIfNoPresetsConfiguredForBackend();
                }
                catch (Exception e)
                {
                    this.notificationService.NotifyError(
                        string.Format(Strings.Get("BackendSwitchFailed"), kind.GetFormattedName()),
                        e.Message);

                    // 写回配置，避免下次启动继续卡在失败的后端上
                    if (this.config.Backend == kind)
                    {
                        this.config.Backend = this.currentKind;
                        this.config.Save();
                    }

                    // 首次切换就失败时，尝试退回 Edge TTS，保证至少能出声
                    if (Backend == null && !this.fallbackAttempted && kind != TTSBackend.EdgeTts)
                    {
                        this.fallbackAttempted = true;
                        this.config.Backend = TTSBackend.EdgeTts;
                        this.config.Save();
                        SetBackend(TTSBackend.EdgeTts);
                    }
                }
                finally
                {
                    BackendLoading = false;
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void WarnIfNoPresetsConfiguredForBackend()
        {
            if (!this.config.Enabled ||
                this.config.Backend.IsRelayBackend() ||
                this.config.GetVoiceConfig().VoicePresets.Any(vp => vp.EnabledBackend == this.config.Backend))
            {
                return;
            }

            this.notificationService.NotifyWarning(
                Strings.Get("BackendNoPresetsConfigured"),
                Strings.Get("BackendCreatePresetHint"));
        }

        public Vector4 GetBackendTitleBarColor()
        {
            return Backend?.TitleBarColor ?? TitleBarColor;
        }

        private VoiceBackend CreateBackendFor(TTSBackend backendKind)
        {
            return backendKind switch
            {
                TTSBackend.System => new SystemBackend(this.config, this.http),
                TTSBackend.Websocket => new WebsocketBackend(this.config, this.notificationService),
                TTSBackend.AmazonPolly => new PollyBackend(this.config, this.http),
                TTSBackend.Uberduck => new UberduckBackend(this.config, this.http),
                TTSBackend.Azure => new AzureBackend(this.config, this.http),
                TTSBackend.ElevenLabs => new ElevenLabsBackend(this.config, this.http, this.notificationService),
                TTSBackend.OpenAi => new OpenAiBackend(this.config, this.http, this.notificationService),
                TTSBackend.GoogleCloud => new GoogleCloudBackend(this.config),
                TTSBackend.Kokoro => new KokoroBackend(this.config),
                TTSBackend.Megaphone => new MegaphoneBackend(this.config, this.notificationService),
                TTSBackend.FishAudio => new FishAudioBackend(this.config, this.http, this.notificationService),
                TTSBackend.EdgeTts => new EdgeTtsBackend(this.config, this.http, this.notificationService),
                TTSBackend.FreeWeb => new FreeWebTtsBackend(this.config, this.http, this.notificationService),
                _ => throw new ArgumentOutOfRangeException(nameof(backendKind)),
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Backend?.Dispose();
            }
        }
    }
}
