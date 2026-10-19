using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using TextToTalk.UI.Windows;
using TextToTalk.Resources;

namespace TextToTalk.Backends.ElevenLabs
{
    public class ElevenLabsVoiceStyles : IVoiceStylesWindow
    {
        private readonly ElevenLabsBackend backend;
        private PluginConfiguration config;
        private bool showVoiceStyles = false;
        private string newStyleBuffer = string.Empty;
        static double lastCopyTime = -1.0;
        static string lastCopiedStyle = "";
        public ElevenLabsVoiceStyles(ElevenLabsBackend backend, PluginConfiguration config)
        {
            this.backend = backend;
            this.config = config;
        }

        public void Draw(IConfigUIDelegates helpers)
        {
            bool shouldAdd = false;
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.0f, 1.0f));
            ImGui.TextWrapped(Strings.Get("ElevenLabsStylesNote"));
            ImGui.PopStyleColor();
            ImGui.TextDisabled(Strings.Get("ElevenLabsStylesHaveFun"));


            if (ImGui.InputText("##StyleInput", ref newStyleBuffer, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                shouldAdd = true;
            }

            ImGui.SameLine();
            if (ImGui.Button(Strings.Get("ElevenLabsStylesAdd")) && !string.IsNullOrWhiteSpace(newStyleBuffer))
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
                ImGui.Text(Strings.Get("ElevenLabsStylesCopyHint"));
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
                        ImGui.SetTooltip(Strings.Get("ElevenLabsStylesCopied"));
                    else if (ImGui.IsItemHovered() && config.AdHocStyleTagsEnabled)
                        ImGui.SetTooltip(Strings.Get("ElevenLabsStylesClickToCopy"));

                    if (ImGui.BeginPopupContextItem($"context_{i}"))
                    {
                        if (ImGui.MenuItem(Strings.Get("ElevenLabsStylesRemoveStyle")))
                            indexToRemove = i;

                        ImGui.EndPopup();
                    }
                }

                if (indexToRemove != -1)
                    config.CustomVoiceStyles.RemoveAt(indexToRemove);
            }
            else
            {
                ImGui.TextDisabled(Strings.Get("ElevenLabsStylesNoStylesYet"));
            }
        }
    }
}