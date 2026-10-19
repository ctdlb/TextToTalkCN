using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using TextToTalk.Lexicons;
using TextToTalk.Lexicons.Updater;
using TextToTalk.Localization;
using TextToTalk.Resources;
using TextToTalk.UI;
using TextToTalk.UI.Lexicons;
using TextToTalk.UI.Windows;
using System.Numerics;

namespace TextToTalk.Backends.Azure;

public class AzureBackendUI
{
    private readonly PluginConfiguration config;
    private readonly LexiconComponent lexiconComponent;
    private readonly AzureBackendUIModel model;
    private readonly AzureBackend backend;

    private string region;
    private string subscriptionKey;
    private bool chineseOnly = true;
    private string testText = string.Empty;

    public AzureBackendUI(AzureBackendUIModel model, PluginConfiguration config, LexiconManager lexiconManager,
        HttpClient http, AzureBackend backend)
    {
        this.model = model;

        // TODO: Make this configurable
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var downloadPath = Path.Join(appData, "TextToTalk");
        var lexiconRepository = new LexiconRepository(http, downloadPath);

        this.backend = backend;
        this.config = config;
        this.lexiconComponent =
            new LexiconComponent(lexiconManager, lexiconRepository, config, () => config.AzureLexiconFiles);

        (region, subscriptionKey) = this.model.GetLoginInfo();
    }

    public void DrawSettings(IConfigUIDelegates helpers)
    {
        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("AzureRegion"), ref region, 100);
        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("AzureSubscriptionKey"), ref subscriptionKey, 100,
            ImGuiInputTextFlags.Password);

        if (ImGui.Button($"{Strings.Get("AzureSaveAndLogin")}##{MemoizedId.Create()}"))
        {
            this.model.LoginWith(region, subscriptionKey);
        }

        ImGui.SameLine();
        if (ImGui.Button($"{Strings.Get("AzureRegister")}##{MemoizedId.Create()}"))
        {
            WebBrowser.Open(
                "https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/index-text-to-speech");
        }

        ImGui.TextColored(ImColor.HintColor, Strings.Get("AzureCredentialsSecured"));

        var loginError = this.model.AzureLoginException?.Message;
        if (loginError != null)
        {
            ImGui.TextColored(ImColor.Red, string.Format(Strings.Get("AzureLoginFailedFormat"), loginError));
        }

        ImGui.Spacing();

        var currentVoicePreset = this.model.GetCurrentVoicePreset();

        var presets = this.config.GetVoicePresetsForBackend(TTSBackend.Azure).ToList();
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
            ImGui.TextColored(ImColor.Red, string.Format(Strings.Get("AzureNoPresetsFormat"),
                Strings.Get("ActionNewPreset")));
        }
        else if (currentVoicePreset == null && presets.Count > 0)
        {
            config.SetCurrentVoicePreset(presets.First().Id);
        }

        BackendUI.NewPresetButton<AzureVoicePreset>($"{Strings.Get("ActionNewPreset")}##{MemoizedId.Create()}", this.config);

        if (!presets.Any() || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton(
            $"{Strings.Get("AzureDeletePreset")}##{MemoizedId.Create()}",
            currentVoicePreset,
            TTSBackend.Azure,
            this.config);

        var presetName = currentVoicePreset.Name;
        if (ImGui.InputText($"{Strings.Get("AzurePresetName")}##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            this.config.Save();
        }

        {
            var voices = this.model.Voices;

            ImGui.Checkbox($"{Strings.Get("AzureShowChineseOnly")}##{MemoizedId.Create()}", ref this.chineseOnly);

            var availableVoices = voices
                .Where(v => v != null && !string.IsNullOrEmpty(v.ShortName))
                .Where(v => !this.chineseOnly || IsChineseVoice(v!.ShortName!))
                .OrderBy(v => VoiceDisplayNames.Azure(v.ShortName), StringComparer.CurrentCulture)
                .ToList();

            string?[] voiceArray = availableVoices.Select(v => v.ShortName).ToArray();

            var voiceIndex = Array.IndexOf(voiceArray, currentVoicePreset.VoiceName);
            // 1. Determine if the currently selected voice has styles
            bool previewHasStyles = voiceIndex >= 0 && availableVoices[voiceIndex].Styles?.Count > 1;
            string previewName = voiceIndex >= 0 ? VoiceDisplayNames.Azure(voiceArray[voiceIndex]) : Strings.Get("AzureSelectVoice");

            // 2. Start combo with an empty preview string so we can draw our own
            if (ImGui.BeginCombo($"{Strings.Get("AzureVoice")}##{MemoizedId.Create()}", "", ImGuiComboFlags.HeightLarge))
            {
                for (int i = 0; i < availableVoices.Count; i++)
                {
                    var v = availableVoices[i];
                    bool isSelected = (voiceIndex == i);
                    bool hasStyles = v.Styles?.Count > 1;

                    if (ImGui.Selectable($"##{v.ShortName}_{i}", isSelected))
                    {
                        voiceIndex = i;
                        currentVoicePreset.VoiceName = voiceArray[voiceIndex];
                        this.config.Save();
                    }

                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
                    ImGui.Text(VoiceDisplayNames.Azure(v.ShortName));

                    if (hasStyles)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.55f, 0.75f, 1.0f, 1.0f), Strings.Get("AzureStylesAvailable"));
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // 3. Overlay the custom text on the Combo box itself
            // We calculate the position relative to the last item (the Combo box)
            ImGui.SameLine();
            float comboRectMinX = ImGui.GetItemRectMin().X;
            float comboRectMinY = ImGui.GetItemRectMin().Y;
            float stylePadding = ImGui.GetStyle().FramePadding.X;

            // Move cursor to inside the combo box frame
            ImGui.SetCursorScreenPos(new Vector2(comboRectMinX + stylePadding, comboRectMinY + ImGui.GetStyle().FramePadding.Y - 3.0f));

            // Draw the Name
            ImGui.Text(previewName);

            // Draw the Tag if applicable
            if (previewHasStyles)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.55f, 0.75f, 1.0f, 1.0f), Strings.Get("AzureStylesAvailable"));
            }
            switch (availableVoices.Count)
            {
                case 0:
                    ImGui.TextColored(ImColor.Red, Strings.Get("AzureNoVoices"));
                    break;
                case > 0 when voiceIndex < 0:
                    BackendUI.ImGuiVoiceNotSelected();
                    break;
            }
        }

        static bool IsChineseVoice(string shortName) =>
            shortName.StartsWith("zh-", StringComparison.OrdinalIgnoreCase) ||
            shortName.StartsWith("cmn-", StringComparison.OrdinalIgnoreCase) ||
            shortName.StartsWith("wuu-", StringComparison.OrdinalIgnoreCase) ||
            shortName.StartsWith("yue-", StringComparison.OrdinalIgnoreCase);

        var playbackRate = 100 + currentVoicePreset.PlaybackRate;
        if (ImGui.SliderInt($"{Strings.Get("AzurePlaybackRate")}##{MemoizedId.Create()}", ref playbackRate, 50, 200, "%d%%",
                ImGuiSliderFlags.AlwaysClamp))
        {
            currentVoicePreset.PlaybackRate = playbackRate - 100;
            this.config.Save();
        }

        var volume = (int)(currentVoicePreset.Volume * 100);
        if (ImGui.SliderInt($"{Strings.Get("Volume")}##{MemoizedId.Create()}", ref volume, 0, 200, "%d%%"))
        {
            currentVoicePreset.Volume = (float)Math.Round((double)volume / 100, 2);
            this.config.Save();
        }

        var voiceStyles = new List<string>();
        var voiceDetails = this.backend?.voices?.OrderBy(v => v.ShortName).FirstOrDefault(v => v?.ShortName == currentVoicePreset?.VoiceName);
        // the styles list will always contain at least 1 empty string if there are no styles available
        if (voiceStyles == null || (voiceDetails?.Styles?.Count ?? 0) == 1) 
        {
            ImGui.BeginDisabled();
            if (ImGui.BeginCombo(Strings.Get("AzureStyle"), Strings.Get("AzureNoStylesAvailable")))
            {
                ImGui.EndCombo();
            }

            ImGui.EndDisabled();
        }
        else if (voiceDetails?.Styles != null && voiceDetails.Styles.Count > 0)
        {
            voiceStyles.Add("");
            voiceStyles.AddRange(voiceDetails.Styles);
            var styleIndex = voiceStyles.IndexOf(currentVoicePreset.Style ?? "");
            if (ImGui.Combo($"{Strings.Get("AzureStyle")}##{MemoizedId.Create()}", ref styleIndex, voiceStyles, voiceStyles.Count))
            {
                currentVoicePreset.Style = voiceStyles[styleIndex];
                this.config.Save();
            }
        }
        ImGui.Separator();

        var testMessage = string.IsNullOrWhiteSpace(this.testText)
            ? Strings.Get("AzureTestMessage")
            : this.testText;

        if (ImGui.Button($"{Strings.Get("AzureTest")}##{MemoizedId.Create()}"))
        {
            var voice = currentVoicePreset;
            if (voice is not null)
            {
                var request = new SayRequest
                {
                    Source = TextSource.Chat,
                    Voice = currentVoicePreset,
                    Speaker = "Speaker",
                    Text = testMessage,
                    TextTemplate = testMessage,
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
        if (ImGui.Button($"{Strings.Get("AzureConfigureVoiceStyles")}##{MemoizedId.Create()}"))
        {
            VoiceStyles.Instance?.ToggleStyle();
        }

        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("AzureTestTextHint"), ref this.testText, 256);

        this.lexiconComponent.Draw();
        ImGui.Spacing();

        {
            ConfigComponents.ToggleUseGenderedVoicePresets(
                $"{Strings.Get("AzureUseGenderedVoices")}##{MemoizedId.Create()}",
                this.config);

            ImGui.Spacing();
            if (this.config.UseGenderedVoicePresets)
            {
                BackendUI.GenderedPresetConfig("Azure", TTSBackend.Azure, this.config, presets);
            }
        }
    }
}