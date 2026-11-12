using MediAssist.Web.Components.UI;

namespace MediAssist.Web.Models;

/// <summary>
/// Single mapping from <see cref="ApplicationStatus"/> to user-facing label, badge tone and icon,
/// so every surface (badge, summary, filters) describes a status the same way.
/// </summary>
public static class ApplicationStatusDisplay
{
    public static string Label(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => "Draft",
        ApplicationStatus.Submitted => "Submitted",
        ApplicationStatus.UnderReview => "Under review",
        ApplicationStatus.NeedsCorrection => "Needs correction",
        ApplicationStatus.ReadyForProcessing => "Ready for processing",
        ApplicationStatus.Completed => "Completed",
        ApplicationStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static Badge.BadgeTone Tone(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => Badge.BadgeTone.Neutral,
        ApplicationStatus.Submitted => Badge.BadgeTone.Info,
        ApplicationStatus.UnderReview => Badge.BadgeTone.Info,
        ApplicationStatus.NeedsCorrection => Badge.BadgeTone.Warning,
        ApplicationStatus.ReadyForProcessing => Badge.BadgeTone.Brand,
        ApplicationStatus.Completed => Badge.BadgeTone.Success,
        ApplicationStatus.Cancelled => Badge.BadgeTone.Neutral,
        _ => Badge.BadgeTone.Neutral
    };

    /// <summary>How the process continues (Phase 1 rules), stated plainly; never a promise of approval.</summary>
    public static string? Reassurance(ApplicationStatus status) => status switch
    {
        ApplicationStatus.NeedsCorrection => "Once you upload the corrected copy, your application goes back to review.",
        _ => null
    };

    public static string Icon(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => "pencil",
        ApplicationStatus.Submitted => "send",
        ApplicationStatus.UnderReview => "search",
        ApplicationStatus.NeedsCorrection => "alert-triangle",
        ApplicationStatus.ReadyForProcessing => "list-checks",
        ApplicationStatus.Completed => "check-circle",
        ApplicationStatus.Cancelled => "x-circle",
        _ => "circle"
    };
}
