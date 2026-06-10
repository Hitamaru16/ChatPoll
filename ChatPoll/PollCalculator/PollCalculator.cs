using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace ChatPoll.PollCalculator;

/// <summary>
/// A class containing the logic for calculating poll results from chat messages.
/// </summary>
public class ChatPollCalculator
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    private readonly List<XivChatType> chatTypes =
    [
        XivChatType.Say, XivChatType.Party, XivChatType.Yell, XivChatType.Shout, XivChatType.FreeCompany, XivChatType.Alliance,
        XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8, XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8
    ];

    private readonly Dictionary<string, HashSet<string>> playerAnswers = [];

    public ChatPollCalculator(Plugin mainPlugin)
    {
        plugin = mainPlugin;
        configuration = plugin.Configuration;
    }

    private static string? TryGetSenderName(IHandleableChatMessage msg)
    {
        try
        {
            var prop = msg.GetType().GetProperty("Sender");
            if (prop == null) return null;
            var raw = prop.GetValue(msg);
            if (raw == null) return null;
            if (raw is SeString ss) return ss.TextValue;
            if (raw is string s) return s;
            var tvProp = raw.GetType().GetProperty("TextValue");
            if (tvProp != null)
            {
                var tv = tvProp.GetValue(raw);
                if (tv is string ts) return ts;
            }
        }
        catch { }
        return null;
    }

    private static bool IsAnswerLabel(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var parts = s.Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        if (!string.Equals(parts[0], "answer", StringComparison.OrdinalIgnoreCase)) return false;
        return parts[1].All(char.IsDigit);
    }

    private static string? GetLocalPlayerName()
    {
        try
        {
            var clientState = Plugin.ClientState;
            if (clientState == null) return null;
            var lpProp = clientState.GetType().GetProperty("LocalPlayer");
            if (lpProp == null) return null;
            var lp = lpProp.GetValue(clientState);
            if (lp == null) return null;
            var nameProp = lp.GetType().GetProperty("Name");
            if (nameProp != null)
            {
                var nameObj = nameProp.GetValue(lp);
                if (nameObj != null)
                {
                    var tvProp = nameObj.GetType().GetProperty("TextValue");
                    if (tvProp != null)
                    {
                        var tv = tvProp.GetValue(nameObj) as string;
                        if (!string.IsNullOrEmpty(tv)) return tv;
                    }
                    if (nameObj is string s) return s;
                }
            }
            var lpStr = lp.ToString();
            return string.IsNullOrEmpty(lpStr) ? null : lpStr;
        }
        catch { }
        return null;
    }

    internal void OnChatMessage(IHandleableChatMessage msg)
    {
        var xivType = msg.LogKind;
        if (xivType != chatTypes[configuration.TextChannelIndex]) return;
        var playerName = TryGetSenderName(msg) ?? "<unknown>";
        var local = GetLocalPlayerName();
        if (!string.IsNullOrEmpty(local) && string.Equals(playerName, local, StringComparison.OrdinalIgnoreCase))
            return;

        var rawText = msg.Message.TextValue ?? string.Empty;
        var text = configuration.IsCaseSensitive ? rawText : rawText.ToLowerInvariant();

        var answerCount = configuration.NumberOfAnswersIndex + 2;
        var configuredAnswers = configuration.Answers
            .Take(answerCount)
            .Select(a => a?.Trim() ?? string.Empty)
            .Where(a => !string.IsNullOrEmpty(a))
            .ToList();

        // If aliases are enabled, disallow configured answers that are just a number or "Answer N" to avoid ambiguity
        if (configuration.AliasEnabled)
        {
            configuredAnswers = [.. configuredAnswers.Where(a => !IsAnswerLabel(a))];
        }

        // Prepare tokenization for numeric alias matching (word boundaries)
        var tokenSeparators = new char[] { ' ', '\t', ',', '.', '!', '?', ';', ':', '-', '/', '\\', '(', ')', '[', ']', '{', '}', '"', '\'' };
        var tokens = (configuration.IsCaseSensitive ? rawText : rawText.ToLowerInvariant()).Split(tokenSeparators, StringSplitOptions.RemoveEmptyEntries);

        // Collect matched configured answers (map aliases to configured answers)
        var matchedConfigured = new List<string>();

        for (var i = 0; i < answerCount; i++)
        {
            if (i >= configuration.Answers.Length) break;
            var cfg = configuration.Answers[i]?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(cfg)) continue;

            // if AliasEnabled, create aliases for this index
            var idx = (i + 1).ToString();
            var aliasPhrase = configuration.IsCaseSensitive ? $"Answer {idx}" : $"answer {idx}";
            var aliasNumeric = idx;

            var aliasMatched = false;
            if (configuration.AliasEnabled)
            {
                // numeric alias should match whole token
                if (tokens.Contains(configuration.IsCaseSensitive ? aliasNumeric : aliasNumeric))
                    aliasMatched = true;
                // phrase alias can be matched by containment
                if (!aliasMatched && (configuration.IsCaseSensitive ? rawText : rawText.ToLowerInvariant()).Contains(aliasPhrase))
                    aliasMatched = true;
            }

            // match by text if not alias-only, considering case sensitivity
            var textMatched = false;
            if (!configuration.IsAliasOnly)
            {
                if (configuration.IsCaseSensitive)
                    textMatched = cfg.Length > 0 && rawText.Contains(cfg);
                else
                    textMatched = cfg.Length > 0 && text.Contains(cfg, StringComparison.InvariantCultureIgnoreCase);
            }

            if (aliasMatched || textMatched)
            {
                // if alias matched, map to the configured answer text (cfg)
                if (!string.IsNullOrEmpty(cfg)) matchedConfigured.Add(cfg);
            }
        }

        if (matchedConfigured.Count == 0)
            return;

        // No previous answer in this poll
        if (!playerAnswers.TryGetValue(playerName, out var answers))
        {
            answers = [];
            playerAnswers[playerName] = answers;

            // First time this player answered in this poll -> increase participant count and persist
            configuration.NumberOfParticipants++;
            configuration.Save();
            Plugin.Log.Information($"ChatPollCalculator: first answer from {playerName}, participants={configuration.NumberOfParticipants}");
        }
        else
        {
            // if entries are not editable, ignore further attempts
            if (!configuration.IsEntryEditable)
            {
                Plugin.Log.Information($"ChatPollCalculator: {playerName} attempted to change answer but editing is disabled.");
                return;
            }
        }

        // Log matched keywords with quotes
        var display = matchedConfigured.Select(k => string.IsNullOrEmpty(k) ? "<empty>" : '"' + k + '"');
        Plugin.Log.Information($"ChatPollCalculator: {playerName} answered {string.Join(", ", display)}, participants={configuration.NumberOfParticipants}");

        // Only handle single-choice for now
        var firstKeyword = matchedConfigured.First();
        if (answers.SetEquals([firstKeyword]))
            return;

        answers.Clear();
        answers.Add(firstKeyword);
    }

    public void StartReading()
    {
        // reset per-poll tracking
        playerAnswers.Clear();
        configuration.NumberOfParticipants = 0;
        configuration.Save();

        Plugin.Log.Information("ChatPollCalculator: StartReading - cleared tracking, subscribing to ChatMessage");
        Plugin.chatGui!.ChatMessage += OnChatMessage;
    }

    public void StopReading()
    {
        Plugin.Log.Information("ChatPollCalculator: StopReading - unsubscribing from ChatMessage");
        Plugin.chatGui!.ChatMessage -= OnChatMessage;
    }

    public void ComputeResults()
    {
        try
        {
            // ensure participant count matches tracked players
            configuration.NumberOfParticipants = playerAnswers.Count;

            var answerCount = configuration.NumberOfAnswersIndex + 2;
            var counts = new int[answerCount];

            // count matches: for each player, check their selected answers against configured answers
            foreach (var kv in playerAnswers)
            {
                var answers = kv.Value;
                foreach (var a in answers)
                {
                    for (var i = 0; i < answerCount; i++)
                    {
                        var configured = configuration.Answers[i];
                        if (string.Equals(configured, a, StringComparison.OrdinalIgnoreCase))
                        {
                            counts[i]++;
                            break;
                        }
                    }
                }
            }

            var totalValid = counts.Sum();

            for (var i = 0; i < answerCount; i++)
            {
                var pct = totalValid > 0 ? (int)Math.Round(counts[i] * 100.0 / totalValid) : 0;
                configuration.AnswersPercentages[i] = pct.ToString();
            }

            // determine winner: no winner if no votes or if multiple answers share the top count
            if (totalValid == 0)
            {
                configuration.PollWinner = "There is no Winner";
            }
            else
            {
                var max = counts.Max();
                var topCount = counts.Count(c => c == max);
                if (max == 0 || topCount != 1)
                {
                    configuration.PollWinner = "There is no Winner";
                }
                else
                {
                    var maxIdx = Array.IndexOf(counts, max);
                    configuration.PollWinner = $"Poll-Winner: {configuration.Answers[maxIdx]}!";
                }
            }

            configuration.Save();
            Plugin.Log.Information($"ChatPollCalculator: Computed results participants={configuration.NumberOfParticipants} totalValid={totalValid}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"ChatPollCalculator: ComputeResults exception: {ex}");
        }
    }
}
