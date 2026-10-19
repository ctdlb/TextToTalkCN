using Dalamud.Bindings.ImGui;
using System.Linq;
using System.Numerics;
using TextToTalk;
using TextToTalk.Backends;
using TextToTalk.Backends.Azure;
using TextToTalk.Resources;
using TextToTalk.UI.Windows;
using static TextToTalk.Backends.Azure.AzureClient;

public class AzureVoiceStyles : IVoiceStylesWindow
{
    private readonly AzureBackend backend;
    private PluginConfiguration config;
    private VoiceStyles voiceStyles;
    static double lastCopyTime = -1.0;
    static string lastCopiedStyle = "";

    public AzureVoiceStyles(AzureBackend backend, PluginConfiguration config, VoiceStyles voiceStyles)
    {
        this.backend = backend;
        this.config = config;
        this.voiceStyles = voiceStyles;

    }

    public void Draw(IConfigUIDelegates helpers)
    {
        var currentVoicePreset = this.config.GetCurrentVoicePreset<AzureVoicePreset>();
        var voiceDetails = this.backend.voices
            .OrderBy(v => v.Name)
            .FirstOrDefault(v => v?.Name == currentVoicePreset?.VoiceName);

        if (voiceDetails?.Styles == null || voiceDetails.Styles.Count == 0)
        {
            ImGui.TextDisabled(Strings.Get("AzureStylesNoStyles"));
            return;
        }
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 1.0f, 1.0f));
        ImGui.TextWrapped(Strings.Get("AzureStylesNote"));
        ImGui.PopStyleColor();
        if (config.AdHocStyleTagsEnabled)
        {
            ImGui.Separator();
            ImGui.Text(Strings.Get("AzureStylesClickToCopy"));
            ImGui.Separator();
        }

            foreach (var style in voiceDetails.Styles)
        {
            if (config.AdHocStyleTagsEnabled)
            {
                if (ImGui.Selectable($"{style}##{voiceDetails.Styles.IndexOf(style)}"))
                {
                    VoiceStyles.Instance?.CopyStyleToClipboard(style);
                    lastCopyTime = ImGui.GetTime();
                    lastCopiedStyle = style;
                }

                if (lastCopiedStyle == style && (ImGui.GetTime() - lastCopyTime < 1.0))
                {
                    ImGui.SetTooltip(Strings.Get("AzureStylesCopied"));
                }
                else if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Get("AzureStylesClickToCopyTooltip"));
                }
            }
        }
    }
}