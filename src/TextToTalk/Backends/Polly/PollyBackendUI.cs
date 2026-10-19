using Amazon.Polly;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using TextToTalk.Lexicons;
using TextToTalk.Lexicons.Updater;
using TextToTalk.Resources;
using TextToTalk.UI;
using TextToTalk.UI.Lexicons;

namespace TextToTalk.Backends.Polly;

public class PollyBackendUI
{
    private readonly PluginConfiguration config;
    private readonly LexiconComponent lexiconComponent;
    private readonly PollyBackendUIModel model;
    private readonly PollyBackend backend;

    private string accessKey;
    private string secretKey;

    public PollyBackendUI(PollyBackendUIModel model, PluginConfiguration config, LexiconManager lexiconManager,
        HttpClient http, PollyBackend backend)
    {
        this.model = model;

        // TODO: Make this configurable
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var downloadPath = Path.Join(appData, "TextToTalk");
        var lexiconRepository = new LexiconRepository(http, downloadPath);

        this.backend = backend;
        this.config = config;
        this.lexiconComponent =
            new LexiconComponent(lexiconManager, lexiconRepository, config, () => config.PollyLexiconFiles);

        (this.accessKey, this.secretKey) = this.model.GetKeyPair();
    }

    public void DrawSettings(IConfigUIDelegates helpers)
    {
        var region = this.model.GetCurrentRegion();
        var regionIndex = Array.IndexOf(this.model.Regions, region.SystemName);
        if (ImGui.Combo($"{Strings.Get("PollyRegion")}##{MemoizedId.Create()}", ref regionIndex, this.model.Regions,
                this.model.Regions.Length))
        {
            this.model.SetCurrentRegion(this.model.Regions[regionIndex]);
        }

        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("PollyAccessKey"), ref this.accessKey, 100,
            ImGuiInputTextFlags.Password);
        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("PollySecretKey"), ref this.secretKey, 100,
            ImGuiInputTextFlags.Password);

        if (ImGui.Button($"{Strings.Get("PollySaveAndLogin")}##{MemoizedId.Create()}"))
        {
            this.model.LoginWith(this.accessKey, this.secretKey);
        }

        ImGui.SameLine();
        if (ImGui.Button($"{Strings.Get("PollyRegister")}##{MemoizedId.Create()}"))
        {
            WebBrowser.Open("https://docs.aws.amazon.com/polly/latest/dg/getting-started.html");
        }

        ImGui.TextColored(ImColor.HintColor, Strings.Get("PollyCredentialsSecured"));

        var loginError = this.model.PollyLoginException?.Message;
        if (loginError != null)
        {
            ImGui.TextColored(ImColor.Red, string.Format(Strings.Get("PollyLoginFailedFormat"), loginError));
        }

        ImGui.Spacing();

        var currentVoicePreset = this.model.GetCurrentVoicePreset();

        var presets = this.config.GetVoicePresetsForBackend(TTSBackend.AmazonPolly).ToList();
        presets.Sort((a, b) => a.Id - b.Id);

        if (presets.Any() && currentVoicePreset != null)
        {
            var presetIndex = presets.IndexOf(currentVoicePreset);
            if (ImGui.Combo($"{Strings.Get("ChannelPreset")}##{MemoizedId.Create()}", ref presetIndex, presets.Select(p => p.Name).ToArray(),
                    presets.Count))
            {
                this.model.SetCurrentVoicePreset(presets[presetIndex].Id);
            }
        }
        else if (currentVoicePreset != null)
        {
            ImGui.TextColored(ImColor.Red, string.Format(Strings.Get("PollyNoPresetsFormat"),
                Strings.Get("ActionNewPreset")));
        }
        else if (currentVoicePreset == null && presets.Count > 0)
        {
            config.SetCurrentVoicePreset(presets.First().Id);
        }

        BackendUI.NewPresetButton<PollyVoicePreset>($"{Strings.Get("ActionNewPreset")}##{MemoizedId.Create()}", this.config);

        if (!presets.Any() || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton(
            $"{Strings.Get("PollyDeletePreset")}##{MemoizedId.Create()}",
            currentVoicePreset,
            TTSBackend.AmazonPolly,
            this.config);

        var presetName = currentVoicePreset.Name;
        if (ImGui.InputText($"{Strings.Get("PollyPresetName")}##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            this.config.Save();
        }

        var engine = this.model.GetCurrentEngine();
        var engineIndex = Array.IndexOf(this.model.Engines, engine.Value);
        if (ImGui.Combo($"{Strings.Get("PollyEngine")}##{MemoizedId.Create()}", ref engineIndex, this.model.Engines,
                this.model.Engines.Length))
        {
            this.model.SetCurrentEngine(this.model.Engines[engineIndex]);
        }

        {
            var voices = this.model.CurrentEngineVoices;
            var voiceArray = voices.Select(v => $"{v.Name} - {v.Gender} ({v.LanguageName})").ToArray();
            var voiceIdArray = voices.Select(v => v.Id).ToArray();
            var voiceIndex = Array.IndexOf(voiceIdArray, currentVoicePreset.VoiceName);
            if (ImGui.Combo($"{Strings.Get("PollyVoice")}##{MemoizedId.Create()}", ref voiceIndex, voiceArray, voices.Count))
            {
                currentVoicePreset.VoiceName = voiceIdArray[voiceIndex];
                this.config.Save();
            }

            switch (voices.Count)
            {
                case 0:
                    ImGui.TextColored(ImColor.Red, Strings.Get("PollyNoVoices"));
                    break;
                case > 0 when !voices.Any(v => v.Id == currentVoicePreset.VoiceName):
                    BackendUI.ImGuiVoiceNotSupported();
                    break;
            }
        }

        var validSampleRates = new[] { "8000", "16000", "22050", "24000" };
        var sampleRate = currentVoicePreset.SampleRate.ToString();
        var sampleRateIndex = Array.IndexOf(validSampleRates, sampleRate);
        if (ImGui.Combo($"{Strings.Get("PollySampleRate")}##{MemoizedId.Create()}", ref sampleRateIndex, validSampleRates,
                validSampleRates.Length))
        {
            currentVoicePreset.SampleRate = int.Parse(validSampleRates[sampleRateIndex]);
            this.config.Save();
        }

        var playbackRate = currentVoicePreset.PlaybackRate;
        if (ImGui.SliderInt($"{Strings.Get("PollyPlaybackRate")}##{MemoizedId.Create()}", ref playbackRate, 20, 200, "%d%%",
                ImGuiSliderFlags.AlwaysClamp))
        {
            currentVoicePreset.PlaybackRate = playbackRate;
            this.config.Save();
        }

        var volume = (int)(currentVoicePreset.Volume * 100);
        if (ImGui.SliderInt($"{Strings.Get("Volume")}##{MemoizedId.Create()}", ref volume, 0, 200, "%d%%"))
        {
            currentVoicePreset.Volume = (float)Math.Round((double)volume / 100, 2);
            this.config.Save();
        }

        if (engine == Engine.Neural && (currentVoicePreset.VoiceName == VoiceId.Matthew ||
                                        currentVoicePreset.VoiceName == VoiceId.Joanna ||
                                        currentVoicePreset.VoiceName == VoiceId.Lupe ||
                                        currentVoicePreset.VoiceName == VoiceId.Amy))
        {
            var domain = currentVoicePreset.AmazonDomainName;
            var newscaster = domain == "news";
            if (ImGui.Checkbox($"{Strings.Get("PollyNewscasterStyle")}##{MemoizedId.Create()}", ref newscaster))
            {
                currentVoicePreset.AmazonDomainName = newscaster ? "news" : "";
                this.config.Save();
            }
        }
        if (ImGui.Button($"{Strings.Get("PollyTest")}##{MemoizedId.Create()}"))
        {
            var voice = currentVoicePreset;
            if (voice is not null)
            {
                var request = new SayRequest
                {
                    Source = TextSource.Chat,
                    Voice = currentVoicePreset,
                    Speaker = "Speaker",
                    Text = $"Hello from Amazon Polly, this is a test message",
                    TextTemplate = "Hello from Amazon Polly, this is a test message",
                    Race = "Hyur",
                    BodyType = GameEnums.BodyType.Adult,
                    Gender = GameEnums.Gender.None,
                    ChatType = XivChatType.Say,
                    Language = ClientLanguage.English,
                };
                backend.CancelSay(TextSource.Chat);
                backend.Say(request);
            }
        }

        this.lexiconComponent.Draw();
        ImGui.Spacing();

        {
            ConfigComponents.ToggleUseGenderedVoicePresets(
                $"{Strings.Get("PollyUseGenderedVoices")}##{MemoizedId.Create()}",
                this.config);

            ImGui.Spacing();
            if (this.config.UseGenderedVoicePresets)
            {
                BackendUI.GenderedPresetConfig("Polly", TTSBackend.AmazonPolly, this.config, presets);
            }
        }
    }
}