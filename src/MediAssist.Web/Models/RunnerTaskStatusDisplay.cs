namespace MediAssist.Web.Models;

/// <summary>Single mapping from <see cref="RunnerTaskStatus"/> to its user-facing label.</summary>
public static class RunnerTaskStatusDisplay
{
    public static string Label(RunnerTaskStatus status) => status switch
    {
        RunnerTaskStatus.Requested => "Requested",
        RunnerTaskStatus.PendingAssignment => "Waiting for a runner",
        RunnerTaskStatus.Assigned => "Assigned",
        RunnerTaskStatus.Accepted => "Accepted",
        RunnerTaskStatus.InProgress => "In progress",
        RunnerTaskStatus.Completed => "Completed",
        RunnerTaskStatus.Declined => "Declined, needs a new runner",
        RunnerTaskStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static Components.UI.Badge.BadgeTone Tone(RunnerTaskStatus status) => status switch
    {
        RunnerTaskStatus.Completed => Components.UI.Badge.BadgeTone.Success,
        RunnerTaskStatus.Declined => Components.UI.Badge.BadgeTone.Danger,
        RunnerTaskStatus.Cancelled => Components.UI.Badge.BadgeTone.Neutral,
        RunnerTaskStatus.Assigned or RunnerTaskStatus.Accepted or RunnerTaskStatus.InProgress => Components.UI.Badge.BadgeTone.Brand,
        _ => Components.UI.Badge.BadgeTone.Info
    };

    public static string Icon(RunnerTaskStatus status) => status switch
    {
        RunnerTaskStatus.Completed => "check-circle",
        RunnerTaskStatus.Declined => "alert-triangle",
        RunnerTaskStatus.Cancelled => "x-circle",
        RunnerTaskStatus.Assigned or RunnerTaskStatus.Accepted or RunnerTaskStatus.InProgress => "route",
        _ => "clock"
    };
}
