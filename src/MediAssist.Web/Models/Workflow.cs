namespace MediAssist.Web.Models;

// Shared application workflow (Milestone 2, Phase D9a). Once submitted, an application lives in the
// shared workflow store (SQLite on the server) so administrators can review it; drafts stay in the
// patient's browser. Every person and reference number is still fictional.

/// <summary>
/// A submitted application as the shared store holds it: the application, its checklist as the
/// reviewer last left it, its history, and a <see cref="Version"/> that rises with every change
/// (a seeded application nobody has touched yet is version 0).
/// </summary>
public sealed record SharedApplication(
    AssistanceApplication Application,
    ApplicationChecklist Checklist,
    IReadOnlyList<ApplicationEvent> Events,
    int Version)
{
    public string Reference => Application.ReferenceNumber;

    public LocalApplication AsLocal() => new(Application, Checklist, Events);
}

/// <summary>An immutable view of everything in the shared store, swapped whole after each change.</summary>
public sealed record WorkflowState(
    IReadOnlyDictionary<string, SharedApplication> Applications,
    IReadOnlyList<DocumentUpload> Uploads,
    IReadOnlyList<PortalNotification> Notifications,
    IReadOnlyDictionary<string, SharedAuthorization> Authorizations,
    IReadOnlyDictionary<string, SharedRunnerRequest> RunnerRequests)
{
    public static WorkflowState Empty { get; } = new(
        new Dictionary<string, SharedApplication>(StringComparer.OrdinalIgnoreCase), [], [],
        new Dictionary<string, SharedAuthorization>(), new Dictionary<string, SharedRunnerRequest>());
}

/// <summary>What a patient submission writes: the new application, its uploads and the confirmation.</summary>
public sealed record Submission(SharedApplication Application, IReadOnlyList<DocumentUpload> Uploads, PortalNotification Notification);

/// <summary>The outcome of a reviewer action: done, refused with a reason, or stale (someone changed it first).</summary>
public sealed record ReviewResult(bool Succeeded, string? Error, bool Conflict = false)
{
    public static ReviewResult Done { get; } = new(true, null);

    public static ReviewResult Refused(string error) => new(false, error);

    public static ReviewResult Stale { get; } = new(false,
        "This application changed while you were looking at it. The latest version is shown; check it and try again.", true);

    public static ReviewResult SaveFailed { get; } = new(false, "The change couldn’t be saved. Try again.");
}

/// <summary>Whether the account may act for a patient, from their latest authorization request (M3.2).</summary>
public enum AuthorizationStanding { NotNeeded, Pending, Approved, Rejected }

/// <summary>Everything the administrator's review page shows for one application.</summary>
public sealed record ReviewDetail(
    SharedApplication Current,
    ApplicationChecklist Checklist,
    IReadOnlyDictionary<string, DocumentUpload> LatestUploads,
    IReadOnlyList<ApplicationEvent> History,
    AuthorizationStanding Authorization)
{
    public AssistanceApplication Application => Current.Application;
}

/// <summary>A checklist as the family and reviewer see it once undecided uploads are counted.</summary>
public static class ChecklistOverlay
{
    /// <summary>
    /// A missing or sent-back document with a new copy that no reviewer has decided on yet is
    /// "being reviewed" (its old reviewer note is answered). Decided uploads change nothing here:
    /// the reviewer's decision is already in the checklist.
    /// </summary>
    public static ApplicationChecklist Apply(ApplicationChecklist checklist, IEnumerable<DocumentUpload> uploads)
    {
        var pending = uploads
            .Where(u => string.Equals(u.ApplicationReference, checklist.ApplicationReference, StringComparison.OrdinalIgnoreCase))
            .GroupBy(u => u.DocumentName)
            .Select(g => g.MaxBy(u => u.Version)!)
            .Where(u => u.ReviewState == UploadReviewState.Pending)
            .Select(u => u.DocumentName)
            .ToHashSet();
        if (pending.Count == 0) return checklist;

        return checklist with
        {
            Items = checklist.Items.Select(i => pending.Contains(i.Name) && DocumentStatusDisplay.NeedsAction(i.Status)
                ? i with { Status = DocumentStatus.PendingReview, ReviewerNote = null }
                : i).ToList()
        };
    }
}

/// <summary>The review rules (Phase 1, section 7), kept free of storage so they can be tested alone.</summary>
public static class ReviewRules
{
    public const int ReasonMin = 10;
    public const int ReasonMax = 300;

    /// <summary>Statuses an administrator works on. Ready-for-processing stays until it is completed.</summary>
    public static IReadOnlyList<ApplicationStatus> Open { get; } =
        [ApplicationStatus.Submitted, ApplicationStatus.UnderReview, ApplicationStatus.NeedsCorrection, ApplicationStatus.ReadyForProcessing];

    /// <summary>A review starts on a submitted application whose patient the account may act for (M3.2: not after a rejection).</summary>
    public static string? StartProblem(ApplicationStatus status, AuthorizationStanding authorization) =>
        status != ApplicationStatus.Submitted ? "Only a submitted application can start review."
        : authorization == AuthorizationStanding.Pending ? "Review the patient's authorization first. The review can start once it is approved."
        : authorization == AuthorizationStanding.Rejected ? "The patient's authorization wasn't approved. The review can start once the family's new request is approved."
        : null;

    /// <summary>
    /// While in review, a copy waiting for a decision can be accepted or sent back; a missing
    /// document can be asked for (sent back with a reason) but never accepted.
    /// </summary>
    public static string? DecideProblem(ApplicationStatus status, ChecklistItem? item, bool accept) =>
        status != ApplicationStatus.UnderReview ? "Documents can only be checked while the application is in review."
        : item is null ? "That document isn't on this application's checklist."
        : item.Status == DocumentStatus.PendingReview ? null
        : !accept && item.Status == DocumentStatus.Missing ? null
        : accept && item.Status == DocumentStatus.Missing ? $"{item.Name} hasn't been sent yet, so it can't be accepted."
        : $"{item.Name} has no copy waiting for a decision.";

    public static string? ReasonProblem(string? reason)
    {
        var r = reason?.Trim() ?? "";
        return r.Length == 0 ? "Say what the family needs to fix, so they can send the right copy."
            : r.Length < ReasonMin ? $"Write at least {ReasonMin} characters so the family knows what to fix."
            : r.Length > ReasonMax ? $"Keep the reason to {ReasonMax} characters or fewer."
            : null;
    }

    /// <summary>Corrections go back once every copy is decided and at least one document was sent back.</summary>
    public static string? CorrectionProblem(ApplicationStatus status, ApplicationChecklist checklist) =>
        status != ApplicationStatus.UnderReview ? "Corrections can only be requested while the application is in review."
        : checklist.Items.Any(i => i.Status == DocumentStatus.PendingReview) ? "Decide on every document waiting for review first."
        : checklist.Items.Any(i => i.Status == DocumentStatus.NeedsCorrection) ? null
        : ReadyProblem(status, checklist) is null ? "Every required document is accepted. Mark the application ready for processing instead."
        : "Choose “Needs a new copy” or “Ask for it” on the documents the family must send first.";

    /// <summary>Ready for processing needs every required document accepted (or not applicable).</summary>
    public static string? ReadyProblem(ApplicationStatus status, ApplicationChecklist checklist)
    {
        if (status != ApplicationStatus.UnderReview) return "Only an application in review can be marked ready for processing.";
        var open = checklist.Items.Count(i => i.Required && i.Status is not (DocumentStatus.Complete or DocumentStatus.NotApplicable));
        return open == 0 ? null
            : open == 1 ? "1 required document isn't accepted yet."
            : $"{open} required documents aren't accepted yet.";
    }

    public static string? CompleteProblem(ApplicationStatus status) =>
        status != ApplicationStatus.ReadyForProcessing ? "Only an application ready for processing can be completed." : null;
}

// ---- Authorization reviews and runner assignment (D9b) ----

/// <summary>
/// An authorization request as the shared store holds it, with what the reviewer needs about the
/// patient (copied when the family asks, since profiles stay in the family's browser) and the
/// family's link to that profile.
/// </summary>
public sealed record SharedAuthorization(
    AuthorizationRequest Request,
    string PatientName,
    string Relationship,
    DateOnly? DateOfBirth,
    string PatientHref,
    int Version)
{
    public string Id => Request.Id;
}

/// <summary>A runner request as the shared store holds it (a seeded one nobody changed is version 0).</summary>
public sealed record SharedRunnerRequest(RunnerRequestItem Request, int Version)
{
    public string Id => Request.Id;
}

/// <summary>A fictional medical runner the administrator can assign (no real people or contact details).</summary>
public sealed record RunnerProfile(string Name, string Area);

/// <summary>Rules for authorization decisions and runner assignment (D9b).</summary>
public static class StaffDecisionRules
{
    /// <summary>The prototype's fictional runners.</summary>
    public static IReadOnlyList<RunnerProfile> Runners { get; } =
    [
        new("Paolo Dizon", "Bayview and Quezon City"),
        new("Liza Manalo", "Northridge"),
        new("Ramil Ocampo", "San Isidro"),
        new("Cora Bautista", "Eastgate and Mandaluyong"),
    ];

    /// <summary>What the family's profile shows for a request's state.</summary>
    public static AuthorizationStatus ProfileStatus(AuthorizationRequestStatus status) => status switch
    {
        AuthorizationRequestStatus.Approved => AuthorizationStatus.Authorized,
        AuthorizationRequestStatus.Rejected => AuthorizationStatus.Rejected,
        _ => AuthorizationStatus.PendingReview
    };

    /// <summary>Approve only a waiting request whose basis still fits the patient (a parent's request needs a patient under 18).</summary>
    public static string? ApproveProblem(AuthorizationRequestStatus status, AuthorizationBasis basis, DateOnly? dateOfBirth, DateOnly today)
    {
        if (status != AuthorizationRequestStatus.Pending) return "This request already has a decision.";
        if (basis != AuthorizationBasis.ParentOfMinor) return null;
        if (dateOfBirth is not { } dob) return "The patient's date of birth is missing, so a parent's request can't be checked.";
        var age = today.Year - dob.Year - (today < dob.AddYears(today.Year - dob.Year) ? 1 : 0);
        return age < 18 ? null : "The patient is 18 or older, so a parent's request doesn't apply. Don't approve it; ask for a signed letter instead.";
    }

    public static string? RejectProblem(AuthorizationRequestStatus status, string? note) =>
        status != AuthorizationRequestStatus.Pending ? "This request already has a decision." : ReviewRules.ReasonProblem(note);

    /// <summary>
    /// A runner can be assigned while the request waits (or was declined), and changed while assigned
    /// but not yet accepted. Once the runner accepts, the errand is theirs (D10).
    /// </summary>
    public static string? AssignProblem(RunnerTaskStatus status, string? currentRunner, string? runner)
    {
        if (runner is null || Runners.All(r => r.Name != runner)) return "Choose a runner from the list.";
        return status switch
        {
            RunnerTaskStatus.Requested or RunnerTaskStatus.PendingAssignment => null,
            RunnerTaskStatus.Declined or RunnerTaskStatus.Assigned when runner == currentRunner =>
                status == RunnerTaskStatus.Declined ? $"{runner} declined this errand. Choose another runner." : $"{runner} is already assigned.",
            RunnerTaskStatus.Declined or RunnerTaskStatus.Assigned => null,
            RunnerTaskStatus.Accepted or RunnerTaskStatus.InProgress => "The runner has accepted this errand, so it can't be reassigned here.",
            _ => "This request is closed."
        };
    }
}
