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
    bool AuthorizationPending);

/// <summary>An optional runner request as the administrator sees it.</summary>
public sealed record RunnerRequestItem(
    string Id,
    string ApplicationReference,
    string Errand,
    string Place,
    DateTimeOffset NeededBy,
    RunnerTaskStatus Status,
    string? RunnerName);

/// <summary>A runner's assigned task: only the minimum information the errand needs (Phase 1 rule).</summary>
public sealed record RunnerTask(
    string Id,
    string ApplicationReference,
    string Errand,
    string Place,
    string District,
    DateTimeOffset Due,
    RunnerTaskStatus Status,
    string ContactFirstName);
