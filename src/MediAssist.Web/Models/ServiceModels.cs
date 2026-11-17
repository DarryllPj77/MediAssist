namespace MediAssist.Web.Models;

// Prototype view models for programs, document checklists and staff work.
// They mirror the Phase 1 conceptual models (AssistanceProgram, DocumentRequirement,
// PatientDocument, RunnerRequest, RunnerAssignment); EF Core entities come later.

/// <summary>An assistance program listed in the directory. Run by a third party, never by MediAssist.</summary>
public sealed record AssistanceProgram(
    string Id,
    string Name,
    string NameFilipino,
    string RunBy,
    string Summary,
    string Covers,
    IReadOnlyList<string> RequiredDocuments);

/// <summary>One line of an application's document checklist.</summary>
public sealed record ChecklistItem(
    string Name,
    DocumentStatus Status,
    bool Required,
    string WhereToGet,
    string? ReviewerNote = null);

public sealed record ApplicationChecklist(
    string ApplicationReference,
    string ProgramName,
    string PatientName,
    IReadOnlyList<ChecklistItem> Items)
{
    public int ReadyCount => Items.Count(i => i.Status is DocumentStatus.Complete or DocumentStatus.NotApplicable);
}

/// <summary>Runner task lifecycle (Phase 1, section 7). Independent of the application status.</summary>
public enum RunnerTaskStatus
{
    Requested,
    PendingAssignment,
    Assigned,
    Accepted,
    InProgress,
    Completed,
    Declined,
    Cancelled
}

/// <summary>A staff member signed in to the Administrator or Runner portal (fictional).</summary>
public sealed record StaffUser(string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>An application waiting in the administrator's review queue.</summary>
public sealed record ReviewQueueItem(
    string ReferenceNumber,
    string PatientName,
    string ProgramName,
    string HospitalName,
    ApplicationStatus Status,
    int DocumentsReady,
    int DocumentsRequired,
    DateTimeOffset WaitingSince,
    bool AuthorizationPending,
    int AwaitingDecision = 0);

/// <summary>
/// An optional runner request; once assigned it is also the runner's task (D10). The optional
/// fields were added in D6 (patient requests), D9b (<see cref="AssignedAt"/>) and D10
/// (<see cref="Steps"/>); they default, so seeded and older records still read.
/// </summary>
public sealed record RunnerRequestItem(
    string Id,
    string ApplicationReference,
    string Errand,
    string Place,
    DateTimeOffset NeededBy,
    RunnerTaskStatus Status,
    string? RunnerName,
    RunnerErrand? Kind = null,
    string? TimeWindow = null,
    string? Notes = null,
    DateTimeOffset? RequestedAt = null,
    DateTimeOffset? ClosedAt = null,
    DateTimeOffset? AssignedAt = null,
    IReadOnlyList<RunnerStep>? Steps = null)
{
    /// <summary>The reason the latest runner gave for declining or handing back, if any.</summary>
    public string? HandBackReason => Status == RunnerTaskStatus.Declined
        ? Steps?.LastOrDefault(s => s.Kind is RunnerStepKind.Declined or RunnerStepKind.HandedBack)?.Note
        : null;

    /// <summary>The runner's note when the errand was done, if any.</summary>
    public string? CompletionNote => Steps?.LastOrDefault(s => s.Kind == RunnerStepKind.Completed)?.Note;
}

/// <summary>What happened to a runner request, in order (D10): who did what, when, and why.</summary>
public enum RunnerStepKind
{
    Assigned,
    Accepted,
    Started,
    Completed,
    Declined,
    HandedBack
}

public sealed record RunnerStep(DateTimeOffset At, RunnerStepKind Kind, string Runner, string? Note = null);

/// <summary>
/// A runner's task as the runner sees it: only what the errand needs (Phase 1 rule) — the errand,
/// place and district, when, the patient's first name, the reference and the family's notes.
/// </summary>
public sealed record RunnerTaskView(SharedRunnerRequest Shared, string District, string ContactFirstName)
{
    public RunnerRequestItem Request => Shared.Request;
    public string Id => Shared.Id;
}
