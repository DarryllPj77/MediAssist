namespace MediAssist.Web.Models;

// Prototype view models for the Patient Portal.
// These mirror the Phase 1 conceptual models but are UI-facing records only;
// EF Core entities will be introduced separately when persistence is added.

/// <summary>Assistance request lifecycle (Phase 1, section 7).</summary>
public enum ApplicationStatus
{
    Draft,
    Submitted,
    UnderReview,
    NeedsCorrection,
    ReadyForProcessing,
    Completed,
    Cancelled
}

/// <summary>Per-document readiness (Phase 1, section 7).</summary>
public enum DocumentStatus
{
    Missing,
    PendingReview,
    Complete,
    NeedsCorrection,
    NotApplicable
}

public enum AuthorizationStatus
{
    Self,
    Authorized,
    PendingReview
}

public enum ActionPriority
{
    High,
    Medium
}

/// <summary>The signed-in account holder (fictional; no authentication in this milestone).</summary>
public sealed record PortalUser(Guid Id, string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>A reusable patient profile managed by the account holder.</summary>
public sealed record PatientProfile(
    Guid Id,
    string FullName,
    string Relationship,
    AuthorizationStatus Authorization);

public sealed record AssistanceApplication(
    string ReferenceNumber,
    Guid PatientId,
    string PatientName,
    string HospitalName,
    string ProgramName,
    ApplicationStatus Status,
    int DocumentsReady,
    int DocumentsRequired,
    DateTimeOffset LastUpdated,
    string NextStep,
    string ProgramId,
    DateTimeOffset CreatedAt);

/// <summary>Something the account holder must do to keep an application moving.</summary>
public sealed record ActionItem(
    string Id,
    ActionPriority Priority,
    string Title,
    string Detail,
    string ApplicationReference,
    string PatientName,
    string ActionLabel,
    string ActionHref,
    DateTimeOffset? DueBy = null);

public sealed record PortalNotification(
    string Id,
    string Title,
    string Message,
    DateTimeOffset CreatedAt,
    bool IsRead);
