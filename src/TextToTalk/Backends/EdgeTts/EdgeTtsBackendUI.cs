using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text;
using TextToTalk.Localization;
using TextToTalk.Resources;
using TextToTalk.UI;

namespace TextToTalk.Backends.EdgeTts;

public class EdgeTtsBackendUI
{
    private readonly PluginConfiguration config;
    private readonly EdgeTtsClient client;
    private readonly EdgeTtsBackend backend;

    private bool chineseOnly = true;
    private bool loading;
    private string testText = string.Empty;

    public EdgeTtsBackendUI(PluginConfiguration config, EdgeTtsClient client, EdgeTtsBackend backend)
    {
        this.config = config;
        this.client = client;
        this.backend = backend;
    }

    public void DrawSettings(IConfigUIDelegates helpers)
    {
        DrawVoiceListStatus();
        ImGui.Separator();

        var presets = this.config.GetVoicePresetsForBackend(TTSBackend.EdgeTts).ToList();
        presets.Sort((a, b) => a.Id - b.Id);

        var currentVoicePreset = this.config.GetCurrentVoicePreset<EdgeTtsVoicePreset>();

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
            ImGui.TextColored(ImColor.Red, string.Format(Strings.Get("EdgeTtsNoPresetsFormat"),
                Strings.Get("ActionNewPreset")));
        }

        BackendUI.NewPresetButton<EdgeTtsVoicePreset>($"{Strings.Get("ActionNewPreset")}##{MemoizedId.Create()}",
            this.config);

        if (!presets.Any() || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton($"{Strings.Get("EdgeTtsDeletePreset")}##{MemoizedId.Create()}",
            currentVoicePreset, TTSBackend.EdgeTts, this.config);

        var presetName = currentVoicePreset.Name;
        if (ImGui.InputText($"{Strings.Get("EdgeTtsPresetName")}##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            this.config.Save();
        }

        DrawVoiceCombo(currentVoicePreset);

        var rate = currentVoicePreset.Rate;
        if (ImGui.SliderInt($"{Strings.Get("EdgeTtsRate")}##{MemoizedId.Create()}", ref rate, -50, 50, "%d%%"))
        {
            currentVoicePreset.Rate = rate;
            this.config.Save();
        }

        var volume = (int)(currentVoicePreset.Volume * 100);
        if (ImGui.SliderInt($"{Strings.Get("Volume")}##{MemoizedId.Create()}", ref volume, 0, 100, "%d%%"))
        {
            currentVoicePreset.Volume = volume / 100f;
            this.config.Save();
        }

        DrawTestControls(currentVoicePreset);

        ImGui.Spacing();
        ConfigComponents.ToggleUseGenderedVoicePresets(
            $"{Strings.Get("EdgeTtsUseGenderedVoices")}##{MemoizedId.Create()}", this.config);

        if (this.config.UseGenderedVoicePresets)
        {
            BackendUI.GenderedPresetConfig("EdgeTts", TTSBackend.EdgeTts, this.config, presets);
        }
    }

    private void DrawVoiceListStatus()
    {
        if (!this.client.VoicesLoaded)
        {
            if (!this.loading)
            {
                this.loading = true;
                _ = this.client.LoadVoicesAsync().ContinueWith(_ => this.loading = false);
            }

            ImGui.TextColored(ImColor.HintColor, Strings.Get("EdgeTtsVoicesLoading"));
            return;
        }

        if (!string.IsNullOrEmpty(this.client.LastError))
        {
            ImGui.TextColored(ImColor.Red,
                string.Format(Strings.Get("EdgeTtsLoadFailedFormat"), this.client.LastError));
        }

        if (ImGui.Button($"{Strings.Get("EdgeTtsRefreshVoices")}##{MemoizedId.Create()}"))
        {
            _ = this.client.LoadVoicesAsync();
        }
    }

    private void DrawVoiceCombo(EdgeTtsVoicePreset currentVoicePreset)
    {
        if (!this.client.VoicesLoaded) return;

        ImGui.Checkbox($"{Strings.Get("EdgeTtsShowChineseOnly")}##{MemoizedId.Create()}", ref this.chineseOnly);

        var voices = this.client.Voices
            .Where(v => !this.chineseOnly || IsChineseVoice(v.ShortName))
            .OrderBy(v => VoiceDisplayNames.Azure(v.ShortName), StringComparer.CurrentCulture)
            .ToList();

        if (voices.Count == 0)
        {
            ImGui.TextColored(ImColor.Red, Strings.Get("EdgeTtsNoVoices"));
            return;
        }

        var names = voices.Select(v => $"{VoiceDisplayNames.Azure(v.ShortName)}").ToArray();
        var index = voices.FindIndex(v => v.ShortName == currentVoicePreset.VoiceName);
        if (index < 0) index = 0;

        if (ImGui.Combo($"{Strings.Get("AzureVoice")}##{MemoizedId.Create()}", ref index, names, names.Length))
        {
            currentVoicePreset.VoiceName = voices[index].ShortName;
            this.config.Save();
        }
    }

    private void DrawTestControls(EdgeTtsVoicePreset currentVoicePreset)
    {
        var testMessage = string.IsNullOrWhiteSpace(this.testText)
            ? Strings.Get("EdgeTtsTestMessage")
            : this.testText;

        if (ImGui.Button($"{Strings.Get("EdgeTtsTest")}##{MemoizedId.Create()}"))
        {
            this.backend.CancelSay(TextSource.Chat);
            this.backend.Say(new SayRequest
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
            });
        }

        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", Strings.Get("EdgeTtsTestTextHint"), ref this.testText, 256);
    }

    private static bool IsChineseVoice(string shortName) =>
        shortName.StartsWith("zh-", StringComparison.OrdinalIgnoreCase) ||
        shortName.StartsWith("cmn-", StringComparison.OrdinalIgnoreCase) ||
        shortName.StartsWith("wuu-", StringComparison.OrdinalIgnoreCase) ||
        shortName.StartsWith("yue-", StringComparison.OrdinalIgnoreCase);
}
