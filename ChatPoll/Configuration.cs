using Dalamud.Configuration;
using System;

namespace ChatPoll;

/// <summary>
/// A class containing Configuration-Data stored by Dalamud.
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string Poll { get; set; } = "";
    public int TextChannelIndex { get; set; } = 0;
    public string[] TextChannelsNames { get; set; } = ["Say", "Party", "Yell", "Shout", "Free Company", "Alliance",
                                                       "CWLS1", "CWLS2", "CWLS3", "CWLS4", "CWLS5", "CWLS6", "CWLS7", "CWLS8",
                                                       "LS1", "LS2", "LS3", "LS4", "LS5", "LS6", "LS7", "LS8"];
    public string[] TextChannelCommands { get; set; } = ["/say", "/party", "/yell", "/shout", "/fc", "/alliance",
                                                         "/cwl1", "/cwl2", "/cwl3", "/cwl4", "/cwl5", "/cwl6", "/cwl7", "/cwl8",
                                                         "/l1", "/l2", "/l3", "/l4", "/l5", "/l6", "/l7", "/l8"];
    public int NumberOfAnswersIndex { get; set; } = 0;
    public string[] NumberOfAnswers { get; set; } = ["2", "3", "4", "5", "6", "7", "8"];
    public string[] Answers { get; set; } = ["", "", "", "", "", "", "", ""];


    public bool IsCaseSensitive { get; set; } = true;
    public bool AliasEnabled { get; set; } = true;
    public bool IsAliasOnly { get; set; } = true;
    public bool IsEntryEditable { get; set; } = true;
    public bool ParticipantsTotalShown { get; set; } = true;
    public int ConfigureTimerDuration { get; set; } = 60;

    public bool PollReady { get; set; } = false;
    public bool PollTransition { get; set; } = false;
    public bool PollRunning { get; set; } = false;
    public bool PollCancelled { get; set; } = false;
    public string PollWinner {  get; set; } = "";
    public string[] AnswersPercentages { get; set; } = ["", "", "", "", "", "", "", ""];
    public int NumberOfParticipants { get; set; } = 0;
    public int TimerDuration { get; set; } = 60;


    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
