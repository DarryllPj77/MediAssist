namespace MediAssist.Web.Models;

public enum StageState
{
    Active,     // moving normally
    Attention,  // waiting on the applicant (needs correction)
    Finished,
    Stopped     // cancelled
}

/// <summary>
/// Display mapping from an application's status to its stage on the five-stage journey
/// (Draft, Submitted, In review, Ready, Completed). Presentation only: it never decides
/// or changes a status.
/// </summary>
public static class ApplicationStageDisplay
{
    public static IReadOnlyList<string> Stages { get; } =
        ["Draft", "Submitted", "In review", "Ready", "Completed"];

    /// <summary>One-based stage number, or 0 when cancelled.</summary>
    public static int Number(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => 1,
        ApplicationStatus.Submitted => 2,
        ApplicationStatus.UnderReview => 3,
        ApplicationStatus.NeedsCorrection => 3,
        ApplicationStatus.ReadyForProcessing => 4,
        ApplicationStatus.Completed => 5,
        _ => 0
    };

    public static StageState State(ApplicationStatus status) => status switch
    {
        ApplicationStatus.NeedsCorrection => StageState.Attention,
        ApplicationStatus.Completed => StageState.Finished,
        ApplicationStatus.Cancelled => StageState.Stopped,
        _ => StageState.Active
    };

    /// <summary>"Step 3 of 5", or "Stopped" for a cancelled application.</summary>
    public static string StepText(ApplicationStatus status) =>
        Number(status) is var n and > 0 ? $"Step {n} of {Stages.Count}" : "Stopped";
}
