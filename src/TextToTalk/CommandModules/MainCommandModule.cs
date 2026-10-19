using System;
using System.Globalization;
using System.Linq;
using Dalamud.Plugin.Services;
using TextToTalk.Backends;
using TextToTalk.Resources;
using TextToTalk.UI;
using TextToTalk.UI.Windows;

namespace TextToTalk.CommandModules;

public class MainCommandModule : CommandModule
{
    private readonly IChatGui chat;

    private readonly PluginConfiguration config;
    private readonly VoiceBackendManager backendManager;
    private readonly ConfigurationWindow configurationWindow;
    private readonly IConfigUIDelegates configUIDelegates;
    private readonly VoiceStyles StylesWindow;

    public MainCommandModule(ICommandManager commandManager, IChatGui chat, PluginConfiguration config,
        VoiceBackendManager backendManager, ConfigurationWindow configurationWindow, IConfigUIDelegates configUIDelegates, VoiceStyles StylesWindow) : base(commandManager) //ElevenLabsStylesWindow elevenLabsStylesWindow)
    {
        this.chat = chat;

        this.config = config;
        this.backendManager = backendManager;
        this.configurationWindow = configurationWindow;
        this.configUIDelegates = configUIDelegates;
        this.StylesWindow = StylesWindow;

        AddCommand("/canceltts", CancelTts, Strings.CommandCancelTts);
        AddCommand("/toggletts", ToggleTts, Strings.CommandToggleTts);
        AddCommand("/disabletts", DisableTts, Strings.CommandDisableTts);
        AddCommand("/enabletts", EnableTts, Strings.CommandEnableTts);
        AddCommand("/tttconfig", ToggleConfig, Strings.CommandToggleConfig);
        AddCommand("/tttstyles", ToggleStyles, Strings.CommandToggleStyles);
        AddCommand("/tttpreset", SwitchPreset, Strings.CommandSwitchPreset);
        AddCommand("/tttvolume", AdjustVolume, Strings.CommandAdjustVolume);
    }

    public void CancelTts(string command = "", string args = "")
    {
        this.backendManager.CancelAllSpeech();
    }

    public void ToggleTts(string command = "", string args = "")
    {
        if (this.config.Enabled)
            DisableTts();
        else
            EnableTts();
    }

    public void DisableTts(string command = "", string args = "")
    {
        this.config.Enabled = false;
        CancelTts();
        this.chat.Print(Strings.TtsDisabledMessage);
        DetailedLog.Info("TTS disabled.");
    }

    public void EnableTts(string command = "", string args = "")
    {
        this.config.Enabled = true;
        this.chat.Print(Strings.TtsEnabledMessage);
        DetailedLog.Info("TTS enabled.");
    }

    public void ToggleConfig(string command = "", string args = "")
    {
        this.configurationWindow.Toggle();
    }
    public void ToggleStyles(string command = "", string args = "")
    {
        this.StylesWindow.Toggle();
    }

    public void SwitchPreset(string command = "", string args = "")
    {
        var trimmed = (args ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            var current = this.config.GetCurrentEnabledChatTypesPreset();
            this.chat.Print(string.Format(Strings.CurrentPresetFormat, current?.Name ?? Strings.UnnamedPreset));
            var names = string.Join(", ",
                this.config.EnabledChatTypesPresets.Select(p => p.Name ?? $"#{p.Id}"));
            this.chat.Print(string.Format(Strings.AvailablePresetsFormat, names));
            return;
        }

        var match = this.config.EnabledChatTypesPresets
            .FirstOrDefault(p => string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            this.chat.Print(string.Format(Strings.NoSuchPresetFormat, trimmed));
            return;
        }

        this.config.SetCurrentEnabledChatTypesPreset(match.Id);
        this.config.Save();
        this.chat.Print(string.Format(Strings.PresetSwitchedFormat, match.Name));
        DetailedLog.Info($"TextToTalk preset -> {match.Name}");
    }

    public void AdjustVolume(string command = "", string args = "")
    {
        var trimmed = (args ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            this.chat.Print(string.Format(Strings.CurrentVolumeFormat, this.config.GlobalVolume * 100f));
            return;
        }

        var current = this.config.GlobalVolume * 100f;
        float target;
        if (trimmed[0] is '+' or '-')
        {
            if (!float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var delta))
            {
                this.chat.Print(string.Format(Strings.InvalidVolumeAdjustFormat, trimmed));
                return;
            }
            target = current + delta;
        }
        else
        {
            if (!float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                this.chat.Print(string.Format(Strings.InvalidVolumeFormat, trimmed));
                return;
            }
            target = value;
        }

        var clamped = Math.Clamp(target, 0f, 200f);
        this.config.GlobalVolume = clamped / 100f;
        this.config.Save();
        this.chat.Print(string.Format(Strings.VolumeSetFormat, clamped));
        DetailedLog.Info($"Volume -> {clamped:0}%");
    }
}