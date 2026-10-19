using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using KokoroSharp;
using KokoroSharp.Core;
using System;
using System.Linq;
using TextToTalk.Resources;
using TextToTalk.UI;

namespace TextToTalk.Backends.Kokoro;

public class KokoroBackendUI(PluginConfiguration config, KokoroBackend kokoroBackend)
{
    public void DrawVoicePresetOptions()
    {
        ImGui.TextColored(ImColor.HintColor, Strings.Get("KokoroPerformanceWarning"));

        ImGui.Spacing();

        var currentVoicePreset = config.GetCurrentVoicePreset<KokoroVoicePreset>();
        var presets = config.GetVoicePresetsForBackend(TTSBackend.Kokoro).ToList();

        if (presets.Count > 0 && currentVoicePreset != null)
        {
            var presetIndex = currentVoicePreset is not null ? presets.IndexOf(currentVoicePreset) : -1;
            if (ImGui.Combo($"{Strings.Get("KokoroVoicePresetLabel")}##{MemoizedId.Create()}", ref presetIndex,
                    presets.Select(p => p.Name).ToArray(), presets.Count))
            {
                config.SetCurrentVoicePreset(presets[presetIndex].Id);
                config.Save();
            }
        }
        else if (currentVoicePreset != null)
        {
            ImGui.TextColored(ImColor.Red, Strings.Get("KokoroNoPresets"));
        }
        else if (currentVoicePreset == null && presets.Count > 0)
        {
            config.SetCurrentVoicePreset(presets.First().Id);
        }

        BackendUI.NewPresetButton<KokoroVoicePreset>($"{Strings.Get("ActionNewPreset")}##{MemoizedId.Create()}", config);

        if (presets.Count == 0 || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton($"{Strings.Get("KokoroDeletePreset")}##{MemoizedId.Create()}",
            currentVoicePreset,
            TTSBackend.Kokoro,
            config);

        ImGui.Separator();

        var presetName = currentVoicePreset.Name;
        if (ImGui.InputText($"{Strings.Get("KokoroPresetName")}##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            config.Save();
        }

        var voices = KokoroVoiceManager.GetVoices(Enum.GetValues<KokoroLanguage>());
        var voiceNameArray = voices.Select(v => v.Name).ToArray();
        var voiceArray = voices.Select(v => $"{v.Name.Substring(3)} - {v.Gender} ({v.Language})").ToArray();
        var voiceIndex = Array.IndexOf(voiceNameArray, currentVoicePreset.InternalName);
        if (ImGui.BeginCombo($"{Strings.Get("KokoroVoice")}##{MemoizedId.Create()}", voiceArray[voiceIndex]))
        {
            foreach (var voice in voices)
            {
                string displayName = $"{voice.Name.Substring(3)} - {voice.Gender} ({voice.Language})";
                if (!ImGui.Selectable(displayName, voice.Name == currentVoicePreset.InternalName)) continue;
                currentVoicePreset.InternalName = voice.Name;
                config.Save();
            }

            ImGui.EndCombo();
        }

        Components.Tooltip(Strings.Get("KokoroVoiceTooltip"));

        var speed = currentVoicePreset.Speed ?? 1f;
        if (ImGui.SliderFloat($"{Strings.Get("KokoroSpeed")}##{MemoizedId.Create()}", ref speed, 0.5f, 2f, "%.2fx"))
        {
            currentVoicePreset.Speed = speed;
            config.Save();
        }
        
        var volume = (int)((currentVoicePreset.Volume ?? 0.6f) * 100);
        if (ImGui.SliderInt($"{Strings.Get("Volume")}##{MemoizedId.Create()}", ref volume, 0, 200, "%d%%"))
        {
            currentVoicePreset.Volume = MathF.Round((float)volume / 100, 2);
            config.Save();
        }

        if (ImGui.Button($"{Strings.Get("KokoroTest")}##{MemoizedId.Create()}"))
        {
            var voice = voices.FirstOrDefault(v => v.Name == currentVoicePreset.InternalName);
            if (voice is not null)
            {
                kokoroBackend.CancelSay(TextSource.Chat);
                kokoroBackend.Say(string.Format(Strings.Get("KokoroTestMessageFormat"), presetName), currentVoicePreset,
                    TextSource.Chat, Dalamud.Game.ClientLanguage.English);
            }
        }

        Components.Tooltip(Strings.Get("KokoroTestTooltip"));

        ImGui.Separator();

        ConfigComponents.ToggleKokoroUseAmericanEnglish($"{Strings.Get("KokoroUseAmericanEnglish")}##{MemoizedId.Create()}", config);
        ConfigComponents.ToggleUseGenderedVoicePresets($"{Strings.Get("KokoroUseGenderedVoices")}##{MemoizedId.Create()}", config);
        ImGui.Spacing();
        if (config.UseGenderedVoicePresets)
        {
            BackendUI.GenderedPresetConfig("Kokoro", TTSBackend.Kokoro, config, presets);
        }
    }
}