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
        AuthorizationStatus.Authorized => "Authorized",
        AuthorizationStatus.PendingReview => "Being reviewed",
        _ => "Account holder"
    };

    public static Badge.BadgeTone Tone(AuthorizationStatus status) => status switch
    {
        AuthorizationStatus.Authorized => Badge.BadgeTone.Success,
        _ => Badge.BadgeTone.Info
    };

    public static string Icon(AuthorizationStatus status) => status switch
    {
        AuthorizationStatus.Authorized => "shield-check",
        _ => "clock"
    };
}
