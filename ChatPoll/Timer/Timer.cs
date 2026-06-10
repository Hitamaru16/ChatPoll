using ChatPoll.PollCalculator;
using ChatPoll.Chat;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ChatPoll.Timer;

/// <summary>
/// A class containing timer functionality & timer-dependent features.
/// </summary>
public class ChatPollTimer
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;
    private ChatPollCalculator calculator;

    private readonly System.Timers.Timer countdownTimer;

    private readonly ChatServer messenger;

    public ChatPollTimer(Plugin mainPlugin)
    {
        countdownTimer = new System.Timers.Timer(1000); // 1 second interval
        countdownTimer.Elapsed += OnTimerElapsed;
        countdownTimer.AutoReset = true;

        messenger = new ChatServer(Plugin.SigScanner);

        calculator = new ChatPollCalculator(mainPlugin);
        plugin = mainPlugin;
        configuration = plugin.Configuration;
    }

    public void StartTimer()
    {
        calculator = new ChatPollCalculator(this.plugin);
        if (configuration.PollRunning)
        {
            return;
        }
        else
        {
            configuration.PollRunning = true;
            configuration.TimerDuration = configuration.ConfigureTimerDuration;
            configuration.Save();

            Task.Run(Poll_Start);
        }
    }

    public void StopTimer()
    {
        configuration.TimerDuration = 1;
        configuration.Save();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        configuration.TimerDuration--;
        configuration.Save();

        if (configuration.TimerDuration == 10)
        {
            Task.Run(async delegate
            {
                await TenSecondsReminder();
            });
        }

        if (configuration.TimerDuration <= 0)
        {
            countdownTimer.Stop();
            if (!configuration.PollCancelled)
            {
                Task.Run(async delegate
                {
                    await HandleTimerCompletion();
                });
            }
            else
            {
                Task.Run(async delegate
                {
                    await CancelledTimerCompletion();
                });
            }
        }
    }
    private async Task Poll_Start()
    {
        configuration.PollTransition = true;
        configuration.Save();

        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]} === ChatPoll ===<se.10>")));

        Thread.Sleep(2000);

        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  Poll-Topic: {configuration.Poll} <se.10>")));

        Thread.Sleep(2000);

        var answers = "";
        for (var i = 0; i < (configuration.NumberOfAnswersIndex + 2); i++)
        {
            answers += $"Answer {i + 1} = {configuration.Answers[i]} | ";
        }
        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  Answers: {answers} <se.10>")));

        Thread.Sleep(2000);

        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  GO! <se.10>")));

        countdownTimer.Start();

        calculator.StartReading();

        configuration.PollTransition = false;
        configuration.Save();
    }

    private async Task TenSecondsReminder()
    {
        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  10 seconds remain! <se.10>")));
    }

    private async Task HandleTimerCompletion()
    {
        calculator?.StopReading();
        calculator?.ComputeResults();
        configuration.PollTransition = true;
        configuration.Save();

        if (configuration.ParticipantsTotalShown)
        {
            Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  Poll finished!  Total Participants: {configuration.NumberOfParticipants} <se.10>")));
        }
        else
        {
            Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  Poll finished! <se.10>")));
        }

        Thread.Sleep(2000);

        var answers = "";
        for (var i = 0; i < (configuration.NumberOfAnswersIndex + 2); i++)
        {
            answers += $"Answer {i + 1} = {configuration.AnswersPercentages[i]}% | ";
        }
        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  Answers: {answers} <se.10>")));

        Thread.Sleep(2000);

        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]} === {configuration.PollWinner} ===<se.8>")));

        configuration.PollRunning = false;
        configuration.PollTransition = false;
        configuration.Save();
    }

    private async Task CancelledTimerCompletion()
    {
        calculator?.StopReading();

        Plugin.Framework.RunOnFrameworkThread(() => messenger.SendMessage(messenger.SanitiseText($"{configuration.TextChannelCommands[configuration.TextChannelIndex]}  Poll cancelled! <se.11>")));

        configuration.PollCancelled = false;
        configuration.PollRunning = false;
        configuration.Save();
    }
}
