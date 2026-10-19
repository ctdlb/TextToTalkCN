using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using TextToTalk;
using TextToTalk.Backends;
using TextToTalk.Backends.OpenAI;
using TextToTalk.Resources;
using TextToTalk.UI.Windows;

public class OpenAIVoiceStyles : IVoiceStylesWindow
{
    private readonly OpenAiBackend backend;
    private PluginConfiguration config;
    private bool showVoiceStyles = false;
    private string newStyleBuffer = string.Empty;
    static double lastCopyTime = -1.0;
    static string lastCopiedStyle = "";
    public OpenAIVoiceStyles(OpenAiBackend backend, PluginConfiguration config)
    {
        this.backend = backend;
        this.config = config;
    }

    public void Draw(IConfigUIDelegates helpers)
    {
        bool shouldAdd = false;
        ImGui.TextDisabled(Strings.Get("OpenAiStylesExperimentHint"));


        if (ImGui.InputText("##StyleInput", ref newStyleBuffer, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            shouldAdd = true;
        }

        ImGui.SameLine();
        if (ImGui.Button(Strings.Get("OpenAiStylesAdd")) && !string.IsNullOrWhiteSpace(newStyleBuffer))
        {
            shouldAdd = true;
        }


        if (shouldAdd && !string.IsNullOrWhiteSpace(newStyleBuffer))
        {
            config.CustomVoiceStyles ??= new List<string>();
            config.CustomVoiceStyles.Add(newStyleBuffer);
            config.CustomVoiceStyles.Sort();
            newStyleBuffer = string.Empty;
        }

        if (config.AdHocStyleTagsEnabled)
        {
            ImGui.Separator();
            ImGui.Text(Strings.Get("OpenAiStylesCopyHint"));
            ImGui.Separator();
        }

        if (config.CustomVoiceStyles?.Count > 0)
        {
            int indexToRemove = -1;

            for (int i = 0; i < config.CustomVoiceStyles.Count; i++)
            {
                string style = config.CustomVoiceStyles[i];
                bool isLastCopied = lastCopiedStyle == style && (ImGui.GetTime() - lastCopyTime < 1.0);

                if (ImGui.Selectable($"{style}##{i}") && config.AdHocStyleTagsEnabled)
                {
                    VoiceStyles.Instance?.CopyStyleToClipboard(style);
                    lastCopyTime = ImGui.GetTime();
                    lastCopiedStyle = style;
                }
                if (isLastCopied)
                    ImGui.SetTooltip(Strings.Get("OpenAiStylesCopied"));
                else if (ImGui.IsItemHovered() && config.AdHocStyleTagsEnabled)
                    ImGui.SetTooltip(Strings.Get("OpenAiStylesClickToCopy"));

                if (ImGui.BeginPopupContextItem($"context_{i}"))
                {
                    if (ImGui.MenuItem(Strings.Get("OpenAiStylesRemoveStyle")))
                        indexToRemove = i;

                    ImGui.EndPopup();
                }
            }

            if (indexToRemove != -1)
                config.CustomVoiceStyles.RemoveAt(indexToRemove);
        }
        else
        {
            ImGui.TextDisabled(Strings.Get("OpenAiStylesNoStyles"));
        }

    }
    
}
