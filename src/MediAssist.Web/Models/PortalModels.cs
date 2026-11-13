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

/// <summary>
/// Whether the account holder may act for a patient (Phase 1: administrator-reviewed). New values
/// are appended so stored numbers keep their meaning.
/// </summary>
public enum AuthorizationStatus
{
    Self,
    Authorized,
    PendingReview,
    NotRequested,
    Rejected
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

/// <summary>
/// A reusable patient profile managed by the account holder. The optional fields were added in D4;
/// they default so older records (seed data, stored snapshots) still read.
/// </summary>
public sealed record PatientProfile(
    Guid Id,
    string FullName,
    string Relationship,
    AuthorizationStatus Authorization,
    DateOnly? DateOfBirth = null,
    string? City = null,
    DateTimeOffset? CreatedAt = null)
{
    /// <summary>Age in whole years on <paramref name="today"/>, when the date of birth is known.</summary>
    public int? AgeOn(DateOnly today) => DateOfBirth is not { } dob ? null
        : today.Year - dob.Year - (today < dob.AddYears(today.Year - dob.Year) ? 1 : 0);
}

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

/// <summary>
/// An in-app notification. <see cref="Href"/> (added in D7, optional so older records still read)
/// points at what it is about: an application, a patient profile or the runner page.
/// </summary>
public sealed record PortalNotification(
    string Id,
    string Title,
    string Message,
    DateTimeOffset CreatedAt,
    bool IsRead,
    string? Href = null);

/// <summary>The user's own read/unread choice for one notification (D7), overriding its default.</summary>
public sealed record NotificationReadState(string Id, bool IsRead);
