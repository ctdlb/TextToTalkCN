using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text;
using TextToTalk.Resources;
using TextToTalk.UI;

namespace TextToTalk.Backends.FreeWeb;

public class FreeWebTtsBackendUI
{
    private readonly PluginConfiguration config;
    private readonly FreeWebTtsClient client;
    private readonly FreeWebTtsBackend backend;

    private string testText = string.Empty;

    public FreeWebTtsBackendUI(PluginConfiguration config, FreeWebTtsClient client, FreeWebTtsBackend backend)
    {
        this.config = config;
        this.client = client;
        this.backend = backend;
    }

    public void DrawSettings(IConfigUIDelegates helpers)
    {
        ImGui.TextColored(ImColor.HintColor, Strings.Get("FreeWebHint"));
        ImGui.Separator();

        var presets = this.config.GetVoicePresetsForBackend(TTSBackend.FreeWeb).ToList();
        presets.Sort((a, b) => a.Id - b.Id);

        var currentVoicePreset = this.config.GetCurrentVoicePreset<FreeWebVoicePreset>();

        if (presets.Any() && currentVoicePreset != null)
        {
            var presetIndex = presets.IndexOf(currentVoicePreset);
            var presetNames = presets.Select(p => p.Name).ToArray();
            if (ImGui.Combo($"{Strings.Get("ChannelPreset")}##{MemoizedId.Create()}", ref presetIndex, presetNames,
                    presetNames.Length))
            {
                this.config.SetCurrentVoicePreset(presets[presetIndex].Id);
            }
        }
        else if (currentVoicePreset != null)
        {
            ImGui.TextColored(ImColor.Red,
                string.Format(Strings.Get("FreeWebNoPresetsFormat"), Strings.Get("ActionNewPreset")));
        }

        BackendUI.NewPresetButton<FreeWebVoicePreset>($"{Strings.Get("ActionNewPreset")}##{MemoizedId.Create()}",
            this.config);

        if (!presets.Any() || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton($"{Strings.Get("FreeWebDeletePreset")}##{MemoizedId.Create()}",
            currentVoicePreset, TTSBackend.FreeWeb, this.config);

        var presetName = currentVoicePreset.Name;
        if (ImGui.InputText($"{Strings.Get("FreeWebPresetName")}##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            this.config.Save();
        }

        DrawProviderCombo(currentVoicePreset);

        var speed = currentVoicePreset.Speed;
        if (ImGui.SliderInt($"{Strings.Get("FreeWebSpeed")}##{MemoizedId.Create()}", ref speed, 1, 9))
        {
            currentVoicePreset.Speed = speed;
            this.config.Save();
        }

        var volume = (int)(currentVoicePreset.Volume * 100);
        if (ImGui.SliderInt($"{Strings.Get("Volume")}##{MemoizedId.Create()}", ref volume, 0, 100, "%d%%"))
        {
            currentVoicePreset.Volume = volume / 100f;
            this.config.Save();
        }

        DrawTestControls(currentVoicePreset);
    }

    private void DrawProviderCombo(FreeWebVoicePreset preset)
    {
        var providers = new[]
        {
            (Key: FreeWebTtsClient.ProviderBaidu, Label: Strings.Get("FreeWebProviderBaidu")),
            (Key: FreeWebTtsClient.ProviderGoogle, Label: Strings.Get("FreeWebProviderGoogle")),
        };

        var providerIndex = Array.FindIndex(providers, p => p.Key == preset.Provider);
        if (providerIndex < 0) providerIndex = 0;

        var labels = providers.Select(p => p.Label).ToArray();
        if (ImGui.Combo($"{Strings.Get("FreeWebProvider")}##{MemoizedId.Create()}", ref providerIndex, labels,
                labels.Length))
        {
            preset.Provider = providers[providerIndex].Key;
            this.config.Save();
        }

        var languages = FreeWebTtsClient.Languages;
        var languageIndex = languages.ToList().FindIndex(l => l.Key == preset.Language);
        if (languageIndex < 0) languageIndex = 0;

        var languageNames = languages.Select(l => l.DisplayName).ToArray();
        if (ImGui.Combo($"{Strings.Get("FreeWebLanguage")}##{MemoizedId.Create()}", ref languageIndex, languageNames,
                languageNames.Length))
        {
            preset.Language = languages[languageIndex].Key;
            this.config.Save();
        }
    }

    private void DrawTestControls(FreeWebVoicePreset preset)
    {
        var testMessage = string.IsNullOrWhiteSpace(this.testText)
            ? Strings.Get("FreeWebTestMessage")
            : this.testText;

        if (ImGui.Button($"{Strings.Get("FreeWebTest")}##{MemoizedId.Create()}"))
        {
            this.backend.CancelSay(TextSource.Chat);
            this.backend.Say(new SayRequest
            {
                Source = TextSource.Chat,
                Voice = preset,
                Speaker = "Speaker",
                Text = testMessage,
                TextTemplate = testMessage,
                Race = "Hyur",
                BodyType = GameEnums.BodyType.Adult,
                Gender = GameEnums.Gender.None,
                ChatType = XivChatType.Say,
                Language = ClientLanguage.English,
            });
        }

        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("FreeWebTestTextHint"), ref this.testText, 256);
    }
}
