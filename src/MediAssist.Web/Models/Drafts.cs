namespace MediAssist.Web.Models;

// The applicant's own work in progress (Milestone 2, Phase D2). Kept per user in the browser
// (see Services/ApplicationWorkspace), never in shared server memory. JSON-serialisable records.

/// <summary>
/// A wizard draft. Choices may still be missing; <see cref="FurthestStep"/> is the last step the
/// user reached (1-5), so completed steps can be revisited. <see cref="SourceReference"/> is set
/// when the draft continues an existing application (for example a seeded draft).
/// </summary>
public sealed record WizardDraft(
    string Id,
    Guid? PatientId,
    string? Hospital,
    string? ProgramId,
    int FurthestStep,
    bool Confirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? SourceReference = null);

/// <summary>An application the user submitted in this browser, with its checklist and history.</summary>
public sealed record LocalApplication(
    AssistanceApplication Application,
    ApplicationChecklist? Checklist,
    IReadOnlyList<ApplicationEvent> Events);

/// <summary>
/// Everything stored for one user. Version 2 (D3) added <see cref="Uploads"/>; version 3 (D4)
/// added <see cref="Profiles"/> (new patients and edits to seeded ones, keyed by id) and
/// <see cref="AuthorizationRequests"/>; version 4 (D6) added <see cref="RunnerRequests"/>. Older
/// snapshots are upgraded on load, never discarded.
/// </summary>
public sealed record WorkspaceSnapshot(
    int Version,
    IReadOnlyList<WizardDraft> Drafts,
    IReadOnlyList<LocalApplication> Submitted,
    IReadOnlyList<PortalNotification> Notifications,
    IReadOnlyList<DocumentUpload>? Uploads = null,
    IReadOnlyList<PatientProfile>? Profiles = null,
    IReadOnlyList<AuthorizationRequest>? AuthorizationRequests = null,
    IReadOnlyList<RunnerRequestItem>? RunnerRequests = null)
{
    public const int CurrentVersion = 4;

    public static WorkspaceSnapshot Empty { get; } = new(CurrentVersion, [], [], [], [], [], [], []);

    /// <summary>Bring an older snapshot up to <see cref="CurrentVersion"/>; null if it cannot be.</summary>
    public WorkspaceSnapshot? Upgrade() => Version is >= 1 and <= CurrentVersion
        ? this with
        {
            Version = CurrentVersion,
            Uploads = Uploads ?? [],
            Profiles = Profiles ?? [],
            AuthorizationRequests = AuthorizationRequests ?? [],
            RunnerRequests = RunnerRequests ?? [],
        }
        : null;
}

/// <summary>Draft ids and application reference numbers for the mock.</summary>
public static class ReferenceGenerator
{
    // No 0/O or 1/I/L, so a draft id can be read aloud or typed back.
    private const string DraftAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>"D-7K3QX2": a draft id not in <paramref name="taken"/>.</summary>
    public static string NewDraftId(IEnumerable<string> taken, Random? random = null)
    {
        random ??= Random.Shared;
        var used = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        string id;
        do
        {
            id = "D-" + string.Concat(Enumerable.Range(0, 6).Select(_ => DraftAlphabet[random.Next(DraftAlphabet.Length)]));
        }
        while (used.Contains(id));
        return id;
    }

    /// <summary>
    /// "MA-2026-00453": the year of <paramref name="now"/> and one more than the highest number
    /// in <paramref name="existing"/>, so it never repeats a reference the prototype knows.
    /// </summary>
    public static string NextReference(IEnumerable<string> existing, DateTimeOffset now)
    {
        var highest = existing
            .Select(r => r.Split('-'))
            .Where(p => p.Length == 3 && p[0] == "MA" && int.TryParse(p[2], out _))
            .Select(p => int.Parse(p[2]))
            .DefaultIfEmpty(0)
            .Max();
        return $"MA-{now.Year}-{highest + 1:D5}";
    }
}
