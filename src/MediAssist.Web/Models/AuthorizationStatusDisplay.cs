using MediAssist.Web.Components.UI;

namespace MediAssist.Web.Models;

/// <summary>
/// Single mapping from <see cref="AuthorizationStatus"/> (Phase 1: administrator-reviewed
/// authorization to act for a patient) to its label, badge tone and icon.
/// </summary>
public static class AuthorizationStatusDisplay
{
    public static string Label(AuthorizationStatus status) => status switch
    {
        AuthorizationStatus.Self => "Account holder",
        AuthorizationStatus.Authorized => "Authorized",
        AuthorizationStatus.PendingReview => "Being reviewed",
        AuthorizationStatus.NotRequested => "Authorization needed",
        AuthorizationStatus.Rejected => "Not approved",
        _ => status.ToString()
    };

    public static Badge.BadgeTone Tone(AuthorizationStatus status) => status switch
    {
        AuthorizationStatus.Authorized => Badge.BadgeTone.Success,
        AuthorizationStatus.NotRequested => Badge.BadgeTone.Warning,
        AuthorizationStatus.Rejected => Badge.BadgeTone.Danger,
        AuthorizationStatus.Self => Badge.BadgeTone.Neutral,
        _ => Badge.BadgeTone.Info
    };

    public static string Icon(AuthorizationStatus status) => status switch
    {
        AuthorizationStatus.Authorized => "shield-check",
        AuthorizationStatus.NotRequested => "alert-triangle",
        AuthorizationStatus.Rejected => "x-circle",
        AuthorizationStatus.Self => "user-circle",
        _ => "clock"
    };

    /// <summary>What the account holder should do next for a patient, or null when nothing.</summary>
    public static string? NextStep(AuthorizationStatus status) => status switch
    {
        AuthorizationStatus.NotRequested => "Request authorization before applying for this patient.",
        AuthorizationStatus.Rejected => "Your last request was not approved. You can send a new one.",
        AuthorizationStatus.PendingReview => "An administrator is reviewing your request. You can already start applications.",
        _ => null
    };
}
