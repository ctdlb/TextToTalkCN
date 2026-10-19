using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text;
using System;
using System.Linq;
using System.Numerics;
using TextToTalk.UI;
using TextToTalk.UI.Windows;
using TextToTalk.Resources;

namespace TextToTalk.Backends.FishAudio;

public class FishAudioBackendUI
{
    private static readonly string[] Models = { "s2.1-pro-free", "s2.1-pro", "s2-pro", "s1" };
    private static readonly string[] Latencies = { "normal", "low", "balanced" };

    private readonly FishAudioBackendUIModel model;
    private readonly PluginConfiguration config;
    private readonly FishAudioBackend backend;

    private string apiKey;

    public FishAudioBackendUI(FishAudioBackendUIModel model, PluginConfiguration config, FishAudioBackend backend)
    {
        this.model = model;
        this.config = config;
        this.apiKey = this.model.GetApiKey();
        this.backend = backend;
    }

    public void DrawSettings()
    {
        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("FishAudioApiKey"), ref this.apiKey, 100,
            ImGuiInputTextFlags.Password);

        if (ImGui.Button($"{Strings.Get("FishAudioSaveAndLogin")}##{MemoizedId.Create()}"))
        {
            this.model.LoginWith(this.apiKey);
        }

        ImGui.SameLine();
        if (ImGui.Button($"{Strings.Get("FishAudioRegister")}##{MemoizedId.Create()}"))
        {
            WebBrowser.Open("https://fish.audio/app/api-keys");
        }

        ImGui.TextColored(ImColor.HintColor, Strings.Get("FishAudioCredentialsSecured"));

        var loginError = this.model.FishAudioLoginException?.Message;
        if (loginError != null)
        {
            ImGui.TextColored(ImColor.Red, string.Format(Strings.Get("FishAudioLoginFailedFormat"), loginError));
        }

        ImGui.Spacing();

        if (this.model.ApiCreditInfo != null)
        {
            ImGui.Text(string.Format(Strings.Get("FishAudioApiCreditFormat"), this.model.ApiCreditInfo.Credit));
            ImGui.Spacing();
        }

        var currentVoicePreset = this.model.GetCurrentVoicePreset();

        var presets = this.config.GetVoicePresetsForBackend(TTSBackend.FishAudio).ToList();
        presets.Sort((a, b) => a.Id - b.Id);

        if (presets.Any() && currentVoicePreset != null)
        {
            var presetIndex = presets.IndexOf(currentVoicePreset);
            if (ImGui.Combo($"{Strings.Get("ChannelPreset")}##{MemoizedId.Create()}", ref presetIndex, presets.Select(p => p.Name ?? "").ToArray(),
                    presets.Count))
            {
                this.model.SetCurrentVoicePreset(presets[presetIndex].Id);
            }
        }
        else if (currentVoicePreset != null)
        {
            ImGui.TextColored(ImColor.Red, Strings.Get("FishAudioNoPresets"));
        }
        else if (currentVoicePreset == null && presets.Count > 0)
        {
            config.SetCurrentVoicePreset(presets.First().Id);
        }

        BackendUI.NewPresetButton<FishAudioVoicePreset>($"{Strings.Get("ActionNewPreset")}##{MemoizedId.Create()}", this.config);

        if (!presets.Any() || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton(
            $"{Strings.Get("FishAudioDeletePreset")}##{MemoizedId.Create()}",
            currentVoicePreset,
            TTSBackend.FishAudio,
            this.config);

        var presetName = currentVoicePreset.Name ?? "";
        if (ImGui.InputText($"{Strings.Get("FishAudioPresetName")}##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            this.config.Save();
        }

        {
            var voiceModels = this.model.VoiceModels;
            var voiceIds = voiceModels.Select(v => v.Id ?? "").ToArray();
            var voiceIndex = Array.IndexOf(voiceIds, currentVoicePreset.VoiceId ?? "");
            var isDefault = string.IsNullOrEmpty(currentVoicePreset.VoiceId);
            var voicePreviewName = isDefault ? Strings.Get("FishAudioDefaultVoiceModel") : (voiceIndex == -1 ? "" : voiceModels[voiceIndex].Title ?? "");
            if (ImGui.BeginCombo($"{Strings.Get("FishAudioVoice")}##{MemoizedId.Create()}", voicePreviewName))
            {
                if (ImGui.Selectable(Strings.Get("FishAudioDefaultVoiceModel"), isDefault))
                {
                    currentVoicePreset.VoiceId = null;
                    this.config.Save();
                }

                if (isDefault) ImGui.SetItemDefaultFocus();

                ImGui.Separator();

                foreach (var voiceModel in voiceModels)
                {
                    if (ImGui.Selectable($"  {voiceModel.Title}"))
                    {
                        currentVoicePreset.VoiceId = voiceModel.Id;
                        this.config.Save();
                    }

                    if (voiceModel.Id == currentVoicePreset.VoiceId)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.TextColored(ImColor.HintColor, Strings.Get("FishAudioVoiceModelsHint"));

            var selectedModelIndex = Array.IndexOf(Models, currentVoicePreset.ModelId ?? "");
            var modelPreview = selectedModelIndex == -1
                ? Strings.Get("FishAudioSelectModel")
                : Models[selectedModelIndex];

            if (ImGui.BeginCombo($"{Strings.Get("FishAudioModel")}##{MemoizedId.Create()}", modelPreview))
            {
                for (int i = 0; i < Models.Length; i++)
                {
                    var isSelected = selectedModelIndex == i;
                    if (ImGui.Selectable($"{Models[i]}##{i}", isSelected))
                    {
                        currentVoicePreset.ModelId = Models[i];
                        this.config.Save();
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            var selectedLatencyIndex = Array.IndexOf(Latencies, currentVoicePreset.Latency ?? "normal");
            if (selectedLatencyIndex == -1) selectedLatencyIndex = 0;
            if (ImGui.Combo($"{Strings.Get("FishAudioLatency")}##{MemoizedId.Create()}", ref selectedLatencyIndex, Latencies, Latencies.Length))
            {
                currentVoicePreset.Latency = Latencies[selectedLatencyIndex];
                this.config.Save();
            }
        }

        var temperature = currentVoicePreset.Temperature;
        if (ImGui.SliderFloat($"{Strings.Get("FishAudioTemperature")}##{MemoizedId.Create()}", ref temperature, 0, 1, "%.2f",
                ImGuiSliderFlags.AlwaysClamp))
        {
            currentVoicePreset.Temperature = temperature;
            this.config.Save();
        }

        var topP = currentVoicePreset.TopP;
        if (ImGui.SliderFloat($"{Strings.Get("FishAudioTopP")}##{MemoizedId.Create()}", ref topP, 0, 1, "%.2f",
                ImGuiSliderFlags.AlwaysClamp))
        {
            currentVoicePreset.TopP = topP;
            this.config.Save();
        }

        var playbackRate = currentVoicePreset.PlaybackRate;
        if (ImGui.SliderInt($"{Strings.Get("FishAudioPlaybackRate")}##{MemoizedId.Create()}", ref playbackRate, 20, 200, "%d%%",
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

        var voiceStyles = config.CustomVoiceStyles.ToList();
        if (voiceStyles.Count == 0)
        {
            ImGui.BeginDisabled();
            if (ImGui.BeginCombo(Strings.Get("FishAudioStyle"), Strings.Get("FishAudioNoStylesConfigured")))
            {
                ImGui.EndCombo();
            }
            ImGui.EndDisabled();
        }
        else
        {
            voiceStyles.Insert(0, "");
            var styleIndex = voiceStyles.IndexOf(currentVoicePreset.Style ?? "");
            if (ImGui.Combo($"{Strings.Get("FishAudioVoiceStyle")}##{MemoizedId.Create()}", ref styleIndex, voiceStyles, voiceStyles.Count))
            {
                currentVoicePreset.Style = voiceStyles[styleIndex];
                this.config.Save();
            }
        }

        if (ImGui.Button($"{Strings.Get("FishAudioTest")}##{MemoizedId.Create()}"))
        {
            if (currentVoicePreset is not null)
            {
                var request = new SayRequest
                {
                    Source = TextSource.Chat,
                    Voice = currentVoicePreset,
                    Speaker = "Speaker",
                    Text = Strings.Get("FishAudioTestMessage"),
                    TextTemplate = Strings.Get("FishAudioTestMessage"),
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

        ImGui.SameLine();
        if (ImGui.Button($"{Strings.Get("FishAudioConfigureVoiceStyles")}##{MemoizedId.Create()}"))
        {
            VoiceStyles.Instance?.ToggleStyle();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Strings.Get("FishAudioEmotionTagsTooltip"));
        }

        {
            ConfigComponents.ToggleUseGenderedVoicePresets(
                $"{Strings.Get("FishAudioUseGenderedVoices")}##{MemoizedId.Create()}",
                this.config);

            ImGui.Spacing();
            if (this.config.UseGenderedVoicePresets)
            {
                BackendUI.GenderedPresetConfig("FishAudio", TTSBackend.FishAudio, this.config, presets);
            }
        }
    }
}
