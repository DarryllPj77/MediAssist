namespace MediAssist.Web.Models;

// Patient profiles and authorization requests (Milestone 2, Phase D4). Fictional data only.

/// <summary>Why the account holder may act for a patient.</summary>
public enum AuthorizationBasis
{
    ParentOfMinor,
    SignedLetter,
    LegalGuardian
}

/// <summary>What an administrator decided about a request (D9 records Approved/Rejected).</summary>
public enum AuthorizationRequestStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// A request to act for a family member, with one simulated supporting document. Reviewed by an
/// administrator in D9, which fills <see cref="DecidedAt"/> and <see cref="ReviewerNote"/>.
/// </summary>
public sealed record AuthorizationRequest(
    string Id,
    Guid PatientId,
    AuthorizationBasis Basis,
    string DocumentFileName,
    string DocumentContentType,
    long DocumentSizeBytes,
    DateTimeOffset RequestedAt,
    AuthorizationRequestStatus Status = AuthorizationRequestStatus.Pending,
    DateTimeOffset? DecidedAt = null,
    string? ReviewerNote = null);

/// <summary>Labels, rules and validation for profiles and authorization requests.</summary>
public static class ProfileRules
{
    public const string SelfRelationship = "You";

    /// <summary>The relationships offered when adding or editing a family member.</summary>
    public static IReadOnlyList<string> Relationships { get; } =
    [
        "Father", "Mother", "Son", "Daughter", "Spouse or partner", "Brother", "Sister",
        "Grandparent", "Grandchild", "Other relative", "Ward (I am the legal guardian)",
    ];

    public static string BasisLabel(AuthorizationBasis basis) => basis switch
    {
        AuthorizationBasis.ParentOfMinor => "I am the parent of a patient under 18",
        AuthorizationBasis.SignedLetter => "The patient signed an authorization letter",
        AuthorizationBasis.LegalGuardian => "I am the patient's legal guardian",
        _ => basis.ToString()
    };

    /// <summary>The document a basis needs (one simulated file).</summary>
    public static string BasisDocument(AuthorizationBasis basis) => basis switch
    {
        AuthorizationBasis.ParentOfMinor => "Birth certificate",
        AuthorizationBasis.SignedLetter => "Signed authorization letter",
        AuthorizationBasis.LegalGuardian => "Guardianship order",
        _ => "Supporting document"
    };

    public static string RequestStatusLabel(AuthorizationRequestStatus status) => status switch
    {
        AuthorizationRequestStatus.Approved => "Approved",
        AuthorizationRequestStatus.Rejected => "Not approved",
        _ => "Being reviewed"
    };

    /// <summary>A profile may (re)request when it has never asked or was not approved.</summary>
    public static bool CanRequest(PatientProfile profile) =>
        profile.Authorization is AuthorizationStatus.NotRequested or AuthorizationStatus.Rejected;

    /// <summary>Applications can be started for oneself, an authorized patient, or one whose request is being reviewed.</summary>
    public static bool CanApplyFor(PatientProfile profile) =>
        profile.Authorization is AuthorizationStatus.Self or AuthorizationStatus.Authorized or AuthorizationStatus.PendingReview;

    /// <summary>Field errors for a profile form, keyed by field name ("name", "relationship", "dob", "city").</summary>
    public static Dictionary<string, string> Validate(string? fullName, string? relationship, DateOnly? dateOfBirth, string? city,
        bool isSelf, DateOnly today)
    {
        var errors = new Dictionary<string, string>();
        var name = fullName?.Trim() ?? "";
        if (name.Length == 0) errors["name"] = "Enter the patient's full name.";
        else if (name.Length < 2 || name.Length > 80) errors["name"] = "Enter a name between 2 and 80 characters.";
        else if (!name.Any(char.IsLetter)) errors["name"] = "Enter a name using letters.";

        if (!isSelf && !Relationships.Contains(relationship ?? "")) errors["relationship"] = "Choose how the patient is related to you.";

        if (dateOfBirth is not { } dob) errors["dob"] = "Enter the patient's date of birth.";
        else if (dob > today) errors["dob"] = "The date of birth can't be in the future.";
        else if (dob < today.AddYears(-120)) errors["dob"] = "Enter a date of birth within the last 120 years.";

        if ((city?.Trim().Length ?? 0) > 60) errors["city"] = "Enter a city or municipality of 60 characters or fewer.";
        return errors;
    }

    /// <summary>Why a basis does not fit this patient, or null when it does.</summary>
    public static string? BasisProblem(AuthorizationBasis? basis, PatientProfile profile, DateOnly today) =>
        basis is null ? "Choose why you may act for this patient."
        : basis == AuthorizationBasis.ParentOfMinor && !(profile.AgeOn(today) is < 18)
            ? $"{profile.FullName} is not under 18, so choose a signed letter or guardianship instead."
        : null;
}
