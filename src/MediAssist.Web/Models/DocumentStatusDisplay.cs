namespace MediAssist.Web.Models;

/// <summary>
/// Single mapping from <see cref="DocumentStatus"/> to its English and Filipino words and icon,
/// so every checklist describes a document the same way.
/// </summary>
public static class DocumentStatusDisplay
{
    public static string Label(DocumentStatus status) => status switch
    {
        DocumentStatus.Complete => "Ready",
        DocumentStatus.PendingReview => "Being reviewed",
        DocumentStatus.NeedsCorrection => "Needs correction",
        DocumentStatus.NotApplicable => "Not needed",
        _ => "Required"
    };

    public static string LabelFilipino(DocumentStatus status) => status switch
    {
        DocumentStatus.Complete => "Ok na",
        DocumentStatus.PendingReview => "Sinusuri",
        DocumentStatus.NeedsCorrection => "Ayusin",
        DocumentStatus.NotApplicable => "Hindi kailangan",
        _ => "Kailangan pa"
    };

    public static string Icon(DocumentStatus status) => status switch
    {
        DocumentStatus.Complete => "check-circle",
        DocumentStatus.PendingReview => "clock",
        DocumentStatus.NotApplicable => "circle",
        _ => "alert-triangle"
    };

    /// <summary>Missing or sent back: the user has something to upload.</summary>
    public static bool NeedsAction(DocumentStatus status) =>
        status is DocumentStatus.Missing or DocumentStatus.NeedsCorrection;
}
