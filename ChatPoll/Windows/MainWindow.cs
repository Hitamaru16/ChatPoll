using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ChatPoll.PollCalculator;
using ChatPoll.Timer;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace ChatPoll.Windows;

/// <summary>
/// A class containing the main window of the plugin
/// </summary>
public class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    public MainWindow(Plugin mainPlugin)
        : base("ChatPoll v1.0.0###MainWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
                                           | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(375, 660);
        configuration = mainPlugin.Configuration;
        plugin = mainPlugin;
    }

    public void Dispose() {}

    public override void Draw()
    {
        var timer = new ChatPollTimer(plugin);
        var reader = new ChatPollCalculator(plugin);

        var messagePreviewSize = new Vector2(360, 200);
        
        using (ImRaii.ItemWidth(140))
        {
            var textChannels = configuration.TextChannelsNames;
            var textChannelIndex = configuration.TextChannelIndex;
            if (ImGui.Combo($"###channelName", ref textChannelIndex, textChannels, textChannels.Length))
            {
                _ = textChannels.ElementAt(textChannelIndex);

                configuration.TextChannelIndex = textChannelIndex;
                configuration.Save();
            }
        }

        ImGui.SameLine();

        ImGui.TextUnformatted("Text-Channel");

        ImGui.Spacing();

        using (ImRaii.ItemWidth(40))
        {
            var numberOfAnswers = configuration.NumberOfAnswers;
            var answerIndex = configuration.NumberOfAnswersIndex;
            if (ImGui.Combo($"###numberOfAnswers", ref answerIndex, numberOfAnswers, numberOfAnswers.Length))
            {
                configuration.NumberOfAnswersIndex = answerIndex;
                configuration.Save();
            }
        }

        var answerCount = configuration.NumberOfAnswersIndex + 2;
        var successCount = new bool[answerCount];

        ImGui.SameLine();

        ImGui.TextUnformatted("Number of Answers");

        ImGui.Spacing();

        using (var child = ImRaii.Child("MainWindowMessagePreview", messagePreviewSize, true))
        {
            if (child.Success)
            {
                ImGui.TextUnformatted("=== ChatPoll ===");
                ImGui.TextUnformatted(" Poll ");
                ImGui.TextUnformatted($"{configuration.Poll}");
                ImGui.TextUnformatted(" Answers ");
                for (var i = 0; i < answerCount; i++)
                {
                    ImGui.TextUnformatted($"Answer {i + 1} = {configuration.Answers[i]} |");
                }
                ImGui.TextUnformatted("");
                ImGui.TextUnformatted("...");
                ImGui.TextUnformatted("");
                ImGui.TextUnformatted("10 Seconds remain!");
                ImGui.TextUnformatted("");
                ImGui.TextUnformatted("...");
                ImGui.TextUnformatted("");
                if (configuration.ParticipantsTotalShown)
                {
                    ImGui.TextUnformatted($" Poll finished!  Total Participants:");
                }
                else
                {
                    ImGui.TextUnformatted(" Poll finished! ");
                }
                for (var i = 0; i < answerCount; i++)
                {
                    ImGui.TextUnformatted($"Answer {i + 1} = % |");
                }
                ImGui.TextUnformatted($" Poll-Winner: ");
            }
        }

        ImGui.Spacing();

        ImGui.TextUnformatted($"Poll-Topic");

        ImGui.SameLine();

        using (ImRaii.ItemWidth(200))
        {
            var poll = configuration.Poll;
            var invalidPoll = "";
            if (configuration.PollRunning)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1)))
                {
                    if (ImGui.InputTextWithHint("###Poll-TopicInactive", $"{configuration.Poll}", ref invalidPoll, ushort.MaxValue))
                    {
                        invalidPoll = "";
                    }
                }
            }
            else
            {
                if (ImGui.InputTextWithHint("###Poll-Topic", "Set the Poll-Topic...", ref poll, ushort.MaxValue))
                {
                    configuration.Poll = poll.Trim();
                    configuration.Save();
                }
            }
        }

        ImGui.SameLine();

        var pollUndetected = false;
        var pollTooBig = false;
        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)))
        {
            if (string.IsNullOrEmpty(configuration.Poll) || string.IsNullOrWhiteSpace(configuration.Poll))
            {
                ImGui.TextUnformatted("Missing!");
                pollUndetected = true;
            }
            else if (configuration.Poll.Length > 350)
            {
                ImGui.TextUnformatted("Too Long!");
                pollTooBig = true;
            }
            else
            {
                ImGui.TextUnformatted("");
                pollUndetected = false;
                pollTooBig = false;
            }
        }

        ImGui.Spacing();

        Dictionary<string, int> frequency = [];
        for (var i = 0; i < answerCount; i++)
        {
            if (!string.IsNullOrWhiteSpace(configuration.Answers[i]))
            {
                if (!frequency.ContainsKey(configuration.Answers[i]))
                    frequency[configuration.Answers[i]] = 0;

                frequency[configuration.Answers[i]]++;
            }
        }

        for (var i = 0; i < answerCount; i++)
        {
            ImGui.TextUnformatted($"Answer {i + 1}");

            ImGui.SameLine();

            using (ImRaii.ItemWidth(200))
            {
                var answers = configuration.Answers;
                var invalidAnswers = "";
                if (configuration.PollRunning)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1)))
                    {
                        if (ImGui.InputTextWithHint($"###Answer{i + 1}Invalid", $"{configuration.Answers[i]}", ref invalidAnswers, ushort.MaxValue))
                        {
                            invalidAnswers = "";
                        }
                    }
                }
                else
                {
                    if (ImGui.InputTextWithHint($"###Answer{i + 1}", $"Set the {i + 1}. answer...", ref answers[i], ushort.MaxValue))
                    {
                        configuration.Answers[i] = answers[i].Trim();
                        configuration.Save();
                    }
                }
            }

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)))
            {
                if (configuration.AliasEnabled && Regex.IsMatch(configuration.Answers[i], @"^Answer \d+$", RegexOptions.IgnoreCase))
                {
                    ImGui.TextUnformatted("Alias-Pattern!");
                    successCount[i] = false;
                }
                else if (string.IsNullOrEmpty(configuration.Answers[i]) || string.IsNullOrWhiteSpace(configuration.Answers[i]))
                {
                    ImGui.TextUnformatted("Missing!");
                    successCount[i] = false;
                }
                else if (!(!frequency.ContainsKey(configuration.Answers[i]) || frequency[configuration.Answers[i]] <= 1))
                {
                    ImGui.TextUnformatted("Already exists!");
                    successCount[i] = false;
                }
                else if (configuration.Answers[i].Length > 30)
                {
                    ImGui.TextUnformatted("Too Long!");
                    successCount[i] = false;
                }
                else
                {
                    ImGui.TextUnformatted("");
                    successCount[i] = true;
                }
            }
        }

        if (successCount.Contains(false) || pollUndetected || pollTooBig)
        {
            configuration.PollReady = false;
            configuration.Save();
        }
        else
        {
            configuration.PollReady = true;
            configuration.Save();
        }

        ImGui.Spacing();

        using (ImRaii.ItemWidth(160))
        {
            if (configuration.PollRunning && !configuration.PollTransition)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1)))
                {
                    ImGui.Button($"Start Poll###PollStartInactive");
                }
            }
            else if (configuration.PollReady && !configuration.PollTransition)
            {
                if (ImGui.Button($"Start Poll###PollStartValid"))
                {
                    timer.StartTimer();
                }
            }
            else
            {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)))
                {
                    ImGui.Button($"Start Poll###PollStartInvalid");
                }
            }
        }

        ImGui.SameLine();

        using (ImRaii.ItemWidth(160))
        {
            if (configuration.PollRunning && !configuration.PollTransition)
            {
                if (ImGui.Button($"Stop Poll###PollStop"))
                {
                    timer.StopTimer();
                    configuration.PollRunning = false;
                    configuration.PollTransition = true;
                    configuration.Save();
                }
            }
            else
            {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1)))
                {
                    ImGui.Button($"Stop Poll###PollStopInactive");
                }
            }
        }

        ImGui.SameLine();

        using (ImRaii.ItemWidth(180))
        {
            if (configuration.PollRunning && !configuration.PollTransition)
            {
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1)))
                {
                    if (ImGui.Button($"Cancel Poll###PollCancel"))
                    {
                        configuration.PollCancelled = true;
                        configuration.PollRunning = false;
                        configuration.Save();
                    }
                }
            }
            else
            {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1)))
                {
                    ImGui.Button($"Cancel Poll###PollCancelInactive");
                }
            }
        }

        ImGui.SameLine();

        if (configuration.PollRunning)
        {
            ImGui.TextUnformatted($"Remaining Time: {configuration.TimerDuration}s");
        }
        else
        {
            ImGui.TextUnformatted("");
        }

            ImGui.Spacing();

        if (configuration.PollReady == false)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)))
            {
                ImGui.TextUnformatted("Input-Editing required.");
            }
        }
        if(!configuration.PollRunning)
        {
            timer.StopTimer();
        }
    }
}

