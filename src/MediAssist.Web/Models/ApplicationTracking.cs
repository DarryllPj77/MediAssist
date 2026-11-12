namespace MediAssist.Web.Models;

// Read models for application tracking (Milestone 2, Phase D1). UI-facing only; when persistence
// arrives, ApplicationEvent maps to an audit/history table and ApplicationDetail to a query.

/// <summary>What happened to an application, for its activity history.</summary>
public enum ApplicationEventKind
{
    Created,
    DraftSaved,
    Submitted,
    ReviewStarted,
    DocumentUploaded,
    CorrectionRequested,
    ReviewerNote,
    ReadyForProcessing,
    RunnerRequested,
    Completed,
    Cancelled
}

/// <summary>
/// One entry in an application's history. <paramref name="Status"/> is set when the event moved
/// the application to a new status; those entries date the stages of the status timeline.
/// </summary>
public sealed record ApplicationEvent(
    string ApplicationReference,
    DateTimeOffset At,
    ApplicationEventKind Kind,
    string Title,
    string Actor,
    string? Detail = null,
    ApplicationStatus? Status = null);

/// <summary>Everything the application detail page shows, assembled by the data service.</summary>
public sealed record ApplicationDetail(
    AssistanceApplication Application,
    PatientProfile? Patient,
    AssistanceProgram? Program,
    ApplicationChecklist? Checklist,
    IReadOnlyList<ApplicationEvent> History,
    IReadOnlyList<RunnerRequestItem> RunnerRequests)
{
    /// <summary>When the application first entered <paramref name="status"/>, if it has.</summary>
    public DateTimeOffset? ReachedAt(ApplicationStatus status) =>
        History.Where(e => e.Status == status).OrderBy(e => e.At).Select(e => (DateTimeOffset?)e.At).FirstOrDefault();
}

/// <summary>
/// The stage groups used to filter applications (dashboard strip and the applications list),
/// so a status is grouped the same way everywhere. Cancelled applications appear under All only.
/// </summary>
public sealed record ApplicationStageFilter(string Key, string Label, string LabelFilipino, IReadOnlyList<ApplicationStatus> Statuses)
{
    public static readonly ApplicationStageFilter All =
        new("all", "All", "Lahat", Enum.GetValues<ApplicationStatus>());

    public static readonly ApplicationStageFilter Draft =
        new("draft", "Drafts", "Borador", [ApplicationStatus.Draft]);

    public static readonly ApplicationStageFilter InReview =
        new("in-review", "Submitted or under review", "Naisumite o sinusuri", [ApplicationStatus.Submitted, ApplicationStatus.UnderReview]);

    public static readonly ApplicationStageFilter NeedsCorrection =
        new("needs-correction", "Needs correction", "Kailangang ayusin", [ApplicationStatus.NeedsCorrection]);

    public static readonly ApplicationStageFilter Finished =
        new("finished", "Ready or completed", "Handa o tapos na", [ApplicationStatus.ReadyForProcessing, ApplicationStatus.Completed]);

    /// <summary>The four stage groups, in journey order (without All).</summary>
    public static IReadOnlyList<ApplicationStageFilter> Stages { get; } = [Draft, InReview, NeedsCorrection, Finished];

    /// <summary>All, then the stage groups: the filter row of the applications list.</summary>
    public static IReadOnlyList<ApplicationStageFilter> WithAll { get; } = [All, .. Stages];

    /// <summary>The filter for a query-string key; unknown or missing keys mean All.</summary>
    public static ApplicationStageFilter FromKey(string? key) =>
        WithAll.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)) ?? All;

    public bool Matches(AssistanceApplication application) => Statuses.Contains(application.Status);

    public bool IsAttention => this == NeedsCorrection;
}
