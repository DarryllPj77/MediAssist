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
}
