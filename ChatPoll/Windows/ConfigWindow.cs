using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace ChatPoll.Windows;

/// <summary>
/// A class containing the configuration window of the plugin
/// </summary>
public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    public ConfigWindow(Plugin mainPlugin) : base("ChatPoll Configurations###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(230, 300);
        SizeCondition = ImGuiCond.Always;
        plugin = mainPlugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var sizeSettingsInput = new Vector2(215, 125);
        var sizeSettingsOutput = new Vector2(215, 95);

        ImGui.TextUnformatted("Input-Settings");

        using (var childInput = ImRaii.Child("Input-Settings", sizeSettingsInput, true))
        {
            if (childInput)
            {
                if (configuration.PollRunning)
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Can't change settings:\nPoll is running!");
                    return;
                }
                var caseValue = configuration.IsCaseSensitive;
                if (ImGui.Checkbox("Case Sensitive", ref caseValue))
                {
                    configuration.IsCaseSensitive = caseValue;
                    configuration.Save();
                }
                ImGuiComponents.HelpMarker("The poll only takes answers which match\nexactly the ones you set.\n\nDoes not affect Aliases.");

                var aliasEnabledValue = configuration.AliasEnabled;
                if (ImGui.Checkbox("Alias Entries", ref aliasEnabledValue))
                {
                    configuration.AliasEnabled = aliasEnabledValue;
                    if (!aliasEnabledValue)
                    {
                        configuration.IsAliasOnly = false;
                    }
                    configuration.Save();
                }
                ImGuiComponents.HelpMarker("Allows the use of aliases as valid entries.\n(Answer x)");

                if (configuration.AliasEnabled)
                {
                    var aliasOnlyValue = configuration.IsAliasOnly;
                    if (ImGui.Checkbox("Alias Only", ref aliasOnlyValue))
                    {
                        configuration.IsAliasOnly = aliasOnlyValue;
                        configuration.Save();
                    }
                }
                else
                {
                    var aliasOnlyValue = false;
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1)))
                        if (ImGui.Checkbox("Alias Only", ref aliasOnlyValue))
                        {
                        }

                    configuration.IsAliasOnly = false;
                    configuration.Save();
                }
                ImGuiComponents.HelpMarker("The poll ONLY accepts aliases as\nvalid entries (Answer x).\n\n(Only available when Aliases are enabled)");

                var entryEditValue = configuration.IsEntryEditable;
                if (ImGui.Checkbox("Changeable Entry", ref entryEditValue))
                {
                    configuration.IsEntryEditable = entryEditValue;
                    configuration.Save();
                }
                ImGuiComponents.HelpMarker("Allows participants to change their\nanswers to the poll.");
            }
        }

        ImGui.TextUnformatted("Output-Settings");

        using var childOutput = ImRaii.Child("Output-Settings", sizeSettingsOutput, true);
        if (childOutput)
        {
            var participantsValue = configuration.ParticipantsTotalShown;
            if (ImGui.Checkbox("Show Total Participants", ref participantsValue))
            {
                configuration.ParticipantsTotalShown = participantsValue;
                configuration.Save();
            }
            ImGuiComponents.HelpMarker("Shows the total amount of participants\nat the end of the poll.");

            ImGui.TextUnformatted("Timer-Duration");

            using (ImRaii.ItemWidth(100))
            {
                var durationValue = configuration.ConfigureTimerDuration;
                if (ImGui.InputInt("Seconds", ref durationValue))
                {
                    var clamped = Math.Clamp(durationValue, 10, 180);
                    configuration.ConfigureTimerDuration = clamped;
                    configuration.Save();
                }
            }
        }
    }
}
