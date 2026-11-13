using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// One user's drafts, submitted applications, simulated document uploads, patient profiles and
/// authorization requests. Registered per
/// circuit (scoped), loaded from that user's browser storage once the page is interactive and
/// written back on every change, so nothing mutable is shared between users. <see cref="Changed"/>
/// tells pages to re-query.
/// <para>
/// The seeded <see cref="MockPortalStore"/> stays read-only; a local entry with the same reference
/// (a resumed or submitted seed draft, or a seeded application whose correction was uploaded)
/// takes its place in the merged view.
/// </para>
/// </summary>
public sealed class ApplicationWorkspace(IWorkspaceStorage storage, MockPortalStore store, TimeProvider clock)
{
    private WorkspaceSnapshot _snapshot = WorkspaceSnapshot.Empty;
    private Task? _loading;

    /// <summary>Raised after the workspace loads or changes. Handlers should marshal to their renderer.</summary>
    public event Action? Changed;

    /// <summary>False until the browser storage has been read (never during prerendering).</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the last save to browser storage failed (the change lives only in this tab).</summary>
    public bool LastSaveFailed { get; private set; }

    public IReadOnlyList<WizardDraft> Drafts => _snapshot.Drafts;
    public IReadOnlyList<LocalApplication> Submitted => _snapshot.Submitted;
    public IReadOnlyList<PortalNotification> Notifications => _snapshot.Notifications;
    public IReadOnlyList<DocumentUpload> Uploads => _snapshot.Uploads ?? [];
    private IReadOnlyList<PatientProfile> LocalProfiles => _snapshot.Profiles ?? [];
    private IReadOnlyList<AuthorizationRequest> LocalRequests => _snapshot.AuthorizationRequests ?? [];

    /// <summary>Read the user's saved workspace once; later calls return the same task.</summary>
    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        _snapshot = await storage.LoadAsync() ?? WorkspaceSnapshot.Empty;
        IsLoaded = true;
        Changed?.Invoke();
    }

    // ---- Patient profiles and authorization requests (D4) ----

    /// <summary>
    /// The account holder's household: the seeded patients (with this user's edits) followed by the
    /// patients they added. Every patient-side scope check uses this list.
    /// </summary>
    public IReadOnlyList<PatientProfile> Household
    {
        get
        {
            var local = LocalProfiles.ToDictionary(p => p.Id);
            var seeded = store.Household.Select(p => local.GetValueOrDefault(p.Id) ?? p).ToList();
            var added = LocalProfiles.Where(p => !store.Household.Any(s => s.Id == p.Id)).OrderBy(p => p.CreatedAt);
            return [.. seeded, .. added];
        }
    }

    public bool IsHousehold(Guid patientId) => Household.Any(p => p.Id == patientId);

    /// <summary>A patient's authorization requests (seeded and this user's), newest first.</summary>
    public IReadOnlyList<AuthorizationRequest> RequestsFor(Guid patientId) =>
        [.. store.AuthorizationRequests().Concat(LocalRequests).Where(r => r.PatientId == patientId).OrderByDescending(r => r.RequestedAt)];

    /// <summary>
    /// Add a patient (id null) or edit one. Validates at this boundary; the account holder stays
    /// "You", and a relationship cannot change while authorization is approved or being reviewed.
    /// Nothing is recorded unless it is saved.
    /// </summary>
    public async Task<ProfileResult> SaveProfileAsync(Guid? id, string fullName, string? relationship, DateOnly? dateOfBirth, string? city)
    {
        var now = clock.GetLocalNow();
        var existing = id is { } known ? Household.FirstOrDefault(p => p.Id == known) : null;
        if (id is not null && existing is null)
            return ProfileResult.Failed(new() { ["name"] = "That patient isn't in your household." });

        var isSelf = existing?.Authorization == AuthorizationStatus.Self;
        var errors = ProfileRules.Validate(fullName, relationship, dateOfBirth, city, isSelf, DateOnly.FromDateTime(now.Date));
        if (existing is not null && !isSelf && relationship != existing.Relationship
            && existing.Authorization is AuthorizationStatus.Authorized or AuthorizationStatus.PendingReview)
            errors["relationship"] = "The relationship can't change while authorization is approved or being reviewed.";
        if (errors.Count > 0) return ProfileResult.Failed(errors);

        var profile = (existing ?? new PatientProfile(Guid.NewGuid(), "", "", AuthorizationStatus.NotRequested, CreatedAt: now)) with
        {
            FullName = fullName.Trim(),
            Relationship = isSelf ? ProfileRules.SelfRelationship : relationship!,
            DateOfBirth = dateOfBirth,
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
        };

        var before = _snapshot;
        _snapshot = _snapshot with { Profiles = [.. LocalProfiles.Where(p => p.Id != profile.Id), profile] };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return ProfileResult.SaveFailed();
        }
        return ProfileResult.Saved(profile);
    }

    /// <summary>
    /// Ask an administrator to confirm the account holder may act for a patient. Needs a basis that
    /// fits the patient, one valid simulated supporting document and a confirmation. The patient
    /// becomes "being reviewed"; a notification confirms it. Nothing is recorded unless it is saved.
    /// </summary>
    public async Task<AuthorizationResult> RequestAuthorizationAsync(Guid patientId, AuthorizationBasis? basis, SampleFile? file, bool confirmed)
    {
        var now = clock.GetLocalNow();
        var patient = Household.FirstOrDefault(p => p.Id == patientId);
        if (patient is null || !ProfileRules.CanRequest(patient))
            return AuthorizationResult.Failed(new() { ["basis"] = "Authorization can't be requested for this patient right now." });

        var errors = new Dictionary<string, string>();
        if (ProfileRules.BasisProblem(basis, patient, DateOnly.FromDateTime(now.Date)) is string basisProblem) errors["basis"] = basisProblem;
        // The document depends on the basis, so it is only asked for once a basis is chosen.
        if (basis is not null && SimulatedFiles.Validate(file) is string fileProblem) errors["file"] = fileProblem;
        if (!confirmed) errors["confirm"] = "Confirm that the information is true before you send the request.";
        if (errors.Count > 0) return AuthorizationResult.Failed(errors);

        var request = new AuthorizationRequest(
            Guid.NewGuid().ToString("N")[..12], patientId, basis!.Value,
            file!.FileName, file.ContentType, file.SizeBytes, now);
        var updated = patient with { Authorization = AuthorizationStatus.PendingReview };
        var notification = new PortalNotification($"n-auth-{request.Id}", "Authorization requested",
            $"Your request to act for {patient.FullName} is being reviewed.", now, false);

        var before = _snapshot;
        _snapshot = _snapshot with
        {
            Profiles = [.. LocalProfiles.Where(p => p.Id != patientId), updated],
            AuthorizationRequests = [.. LocalRequests, request],
            Notifications = [notification, .. _snapshot.Notifications],
        };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return AuthorizationResult.SaveFailed();
        }
        return AuthorizationResult.Saved(request);
    }

    // ---- Drafts ----

    public WizardDraft? FindDraft(string id) =>
        _snapshot.Drafts.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

    public string NewDraftId() =>
        ReferenceGenerator.NewDraftId(_snapshot.Drafts.Select(d => d.Id));

    /// <summary>
    /// Create or update a draft (stamped with the current time) and persist it. If the browser
    /// cannot store it, the draft is kept for this tab and <see cref="LastSaveFailed"/> is set.
    /// </summary>
    public async Task<WizardDraft> SaveDraftAsync(WizardDraft draft)
    {
        var saved = draft with { UpdatedAt = clock.GetLocalNow() };
        _snapshot = _snapshot with { Drafts = [saved, .. _snapshot.Drafts.Where(d => d.Id != saved.Id)] };
        await PersistAsync();
        return saved;
    }

    /// <summary>
    /// Mock submission: the draft becomes a Submitted application with a reference number, a
    /// checklist (missing documents allowed) and its history; a notification confirms it; any
    /// documents uploaded to the draft move to the new reference. Returns the reference, or null
    /// when it could not be saved (nothing changes then). The caller validates the choices first.
    /// </summary>
    public async Task<string?> SubmitAsync(WizardDraft draft)
    {
        var now = clock.GetLocalNow();
        var patient = Household.First(p => p.Id == draft.PatientId);
        var program = store.Programs.First(p => p.Id == draft.ProgramId);
        var seed = draft.SourceReference is null ? null
            : store.Applications().FirstOrDefault(a => a.ReferenceNumber == draft.SourceReference);

        var reference = draft.SourceReference ?? ReferenceGenerator.NextReference(
            store.Applications().Select(a => a.ReferenceNumber).Concat(_snapshot.Submitted.Select(s => s.Application.ReferenceNumber)),
            now);

        var draftKey = draft.SourceReference ?? draft.Id;
        var uploads = Uploads.Select(u => u.ApplicationReference == draftKey ? u with { ApplicationReference = reference } : u).ToList();

        var checklist = ChecklistFor(reference, program, patient.FullName, draft.SourceReference);
        var withUploads = Overlay(checklist, uploads);
        var ready = checklist.ReadyCount;
        var required = checklist.Items.Count;
        var toAdd = withUploads.Items.Count(i => DocumentStatusDisplay.NeedsAction(i.Status));

        var application = new AssistanceApplication(
            reference, patient.Id, patient.FullName, draft.Hospital!, program.Name,
            ApplicationStatus.Submitted, ready, required, now,
            toAdd > 0 ? AddMissing(toAdd) : "Wait for the review to start",
            program.Id, seed?.CreatedAt ?? draft.CreatedAt);

        var sent = required - toAdd;
        IReadOnlyList<ApplicationEvent> history =
        [
            .. EventsFor(draft, reference),
            new(reference, now, ApplicationEventKind.Submitted, "Application submitted", "You",
                toAdd > 0 ? $"Sent with {sent} of {required} documents. The rest can follow." : $"Sent with all {required} documents.",
                ApplicationStatus.Submitted),
        ];

        var notification = new PortalNotification(
            $"n-{reference}", "Application submitted", $"{reference} was sent for review.", now, false);

        var before = _snapshot;
        _snapshot = _snapshot with
        {
            Drafts = _snapshot.Drafts.Where(d => d.Id != draft.Id).ToList(),
            Submitted = [new LocalApplication(application, checklist, history), .. _snapshot.Submitted.Where(s => s.Application.ReferenceNumber != reference)],
            Notifications = [notification, .. _snapshot.Notifications],
            Uploads = uploads,
        };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return null;
        }
        return reference;
    }

    // ---- Simulated uploads (D3) ----

    /// <summary>
    /// Record a simulated upload for one document of an application or draft. Validates the file,
    /// numbers the version (a replacement points at the version it replaces), and, when every
    /// document a reviewer sent back now has a new copy, returns the application to review.
    /// Nothing is recorded unless it is saved.
    /// </summary>
    public async Task<UploadResult> UploadAsync(string reference, string documentName, SampleFile? file)
    {
        if (SimulatedFiles.Validate(file) is string invalid)
            return UploadResult.Invalid(invalid);

        var baseChecklist = BaseChecklist(reference);
        var item = baseChecklist?.Items.FirstOrDefault(i => i.Name == documentName);
        var previous = LatestUpload(reference, documentName);
        if (item is null || !(DocumentStatusDisplay.NeedsAction(item.Status) || previous is not null))
            return UploadResult.Invalid($"{documentName} cannot be uploaded for this application.");

        var now = clock.GetLocalNow();
        var upload = new DocumentUpload(
            Guid.NewGuid().ToString("N")[..12], reference, documentName,
            file!.FileName, file.ContentType, file.SizeBytes, now,
            (previous?.Version ?? 0) + 1, previous?.Id);

        var before = _snapshot;
        _snapshot = _snapshot with { Uploads = [.. Uploads, upload] };

        // Correction round: every document the reviewer sent back has a new copy.
        var corrected = false;
        if (CurrentApplication(reference) is { Status: ApplicationStatus.NeedsCorrection } application
            && baseChecklist!.Items.Where(i => i.Status == DocumentStatus.NeedsCorrection)
                .All(i => LatestUpload(reference, i.Name) is not null))
        {
            ReturnToReview(application, baseChecklist, now);
            corrected = true;
        }

        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return UploadResult.Invalid("We couldn’t save that upload in this browser. Try again.");
        }
        return UploadResult.Saved(upload, corrected);
    }

    /// <summary>The newest upload of a document, if any.</summary>
    public DocumentUpload? LatestUpload(string reference, string documentName) =>
        Uploads.Where(u => string.Equals(u.ApplicationReference, reference, StringComparison.OrdinalIgnoreCase) && u.DocumentName == documentName)
               .MaxBy(u => u.Version);

    public IReadOnlyList<DocumentUpload> UploadsFor(string reference) =>
        Uploads.Where(u => string.Equals(u.ApplicationReference, reference, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// A checklist as it stands after the user's uploads: a missing or sent-back document with a
    /// new copy is "being reviewed" (its old reviewer note is answered and moves to the history).
    /// Readiness is still the reviewer's call, so "Ready" counts do not change.
    /// </summary>
    public ApplicationChecklist WithUploads(ApplicationChecklist checklist) => Overlay(checklist, Uploads);

    /// <summary>History entries for the user's uploads to an application.</summary>
    public IReadOnlyList<ApplicationEvent> UploadEvents(string reference)
    {
        var baseItems = BaseChecklist(reference)?.Items.ToDictionary(i => i.Name, i => i.Status) ?? [];
        return UploadsFor(reference).Select(u =>
        {
            var title = u.Version > 1 ? $"{u.DocumentName} replaced (version {u.Version})"
                : baseItems.GetValueOrDefault(u.DocumentName) == DocumentStatus.NeedsCorrection ? $"Corrected {Lower(u.DocumentName)} uploaded"
                : $"{u.DocumentName} uploaded";
            return new ApplicationEvent(reference, u.UploadedAt, ApplicationEventKind.DocumentUploaded, title, "You",
                $"{u.FileName} · {SimulatedFiles.FormatSize(u.SizeBytes)}");
        }).ToList();
    }

    private static string Lower(string name) => name.Length > 0 ? char.ToLowerInvariant(name[0]) + name[1..] : name;

    private static ApplicationChecklist Overlay(ApplicationChecklist checklist, IEnumerable<DocumentUpload> uploads)
    {
        var uploaded = uploads
            .Where(u => string.Equals(u.ApplicationReference, checklist.ApplicationReference, StringComparison.OrdinalIgnoreCase))
            .Select(u => u.DocumentName)
            .ToHashSet();
        if (uploaded.Count == 0) return checklist;

        return checklist with
        {
            Items = checklist.Items.Select(i => uploaded.Contains(i.Name) && DocumentStatusDisplay.NeedsAction(i.Status)
                ? i with { Status = DocumentStatus.PendingReview, ReviewerNote = null }
                : i).ToList()
        };
    }

    /// <summary>The application (local copy, draft or seed) behind a reference, before uploads.</summary>
    private AssistanceApplication? CurrentApplication(string reference)
    {
        if (_snapshot.Submitted.FirstOrDefault(s => Same(s.Application.ReferenceNumber, reference)) is { } local) return local.Application;
        if (_snapshot.Drafts.FirstOrDefault(d => Same(d.SourceReference ?? d.Id, reference)) is { } draft) return DraftAsApplication(draft);
        return store.Applications().FirstOrDefault(a => Same(a.ReferenceNumber, reference) && IsHousehold(a.PatientId));
    }

    /// <summary>The checklist behind a reference, before uploads.</summary>
    private ApplicationChecklist? BaseChecklist(string reference)
    {
        if (_snapshot.Submitted.FirstOrDefault(s => Same(s.Application.ReferenceNumber, reference)) is { } local) return local.Checklist;
        if (_snapshot.Drafts.FirstOrDefault(d => Same(d.SourceReference ?? d.Id, reference)) is { } draft) return DraftChecklistBase(draft);
        return CurrentApplication(reference) is null ? null : store.Checklists.FirstOrDefault(c => Same(c.ApplicationReference, reference));
    }

    /// <summary>The corrected application goes back to Under review (Phase 1 rule), with history and a notification.</summary>
    private void ReturnToReview(AssistanceApplication application, ApplicationChecklist baseChecklist, DateTimeOffset now)
    {
        var reference = application.ReferenceNumber;
        var existing = _snapshot.Submitted.FirstOrDefault(s => Same(s.Application.ReferenceNumber, reference));
        var events = existing?.Events ?? store.Events().Where(e => e.ApplicationReference == reference).OrderBy(e => e.At).ToList();
        var stillMissing = Overlay(baseChecklist, Uploads).Items.Count(i => DocumentStatusDisplay.NeedsAction(i.Status));

        var updated = application with
        {
            Status = ApplicationStatus.UnderReview,
            LastUpdated = now,
            NextStep = stillMissing > 0 ? AddMissing(stillMissing) : "Wait for the reviewer to check the corrected document",
        };
        IReadOnlyList<ApplicationEvent> history =
        [
            .. events,
            new(reference, now, ApplicationEventKind.CorrectionSent, "Correction sent for review", "You",
                "Every document the reviewer asked for has a new copy, so the application is back in review.",
                ApplicationStatus.UnderReview),
        ];
        var notification = new PortalNotification($"n-{reference}-corrected", "Correction sent",
            $"{reference} is back in review with your corrected document.", now, false);

        _snapshot = _snapshot with
        {
            Submitted = [new LocalApplication(updated, existing?.Checklist ?? baseChecklist, history),
                         .. _snapshot.Submitted.Where(s => !Same(s.Application.ReferenceNumber, reference))],
            Notifications = [notification, .. _snapshot.Notifications.Where(n => n.Id != notification.Id)],
        };
    }

    private static string AddMissing(int count) =>
        count == 1 ? "Add the missing document" : $"Add the {count} missing documents";

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // ---- Drafts as applications, for the tracking pages ----

    /// <summary>A draft shown as an application (status Draft); unchosen fields say so.</summary>
    public AssistanceApplication DraftAsApplication(WizardDraft draft)
    {
        var patient = Household.FirstOrDefault(p => p.Id == draft.PatientId);
        var program = store.Programs.FirstOrDefault(p => p.Id == draft.ProgramId);
        var checklist = DraftChecklistBase(draft);
        var next = Math.Clamp(draft.FurthestStep, 1, 5);
        var stepLabel = StepperSteps.ForWizard(next - 1)[next - 1].Label;

        return new AssistanceApplication(
            draft.SourceReference ?? draft.Id,
            draft.PatientId ?? Guid.Empty,
            patient?.FullName ?? "Patient not chosen yet",
            draft.Hospital ?? "Hospital not chosen yet",
            program?.Name ?? "Program not chosen yet",
            ApplicationStatus.Draft,
            checklist?.ReadyCount ?? 0,
            checklist?.Items.Count ?? 0,
            draft.UpdatedAt,
            $"Finish step {next}: {stepLabel}",
            program?.Id ?? "",
            draft.CreatedAt);
    }

    /// <summary>A draft's checklist with the user's uploads applied.</summary>
    public ApplicationChecklist? DraftChecklist(WizardDraft draft) =>
        DraftChecklistBase(draft) is { } c ? WithUploads(c) : null;

    private ApplicationChecklist? DraftChecklistBase(WizardDraft draft)
    {
        var program = store.Programs.FirstOrDefault(p => p.Id == draft.ProgramId);
        if (program is null) return null;
        var patient = Household.FirstOrDefault(p => p.Id == draft.PatientId);
        return ChecklistFor(draft.SourceReference ?? draft.Id, program, patient?.FullName ?? "the patient", draft.SourceReference);
    }

    /// <summary>A draft's history: the seed's, for a resumed seed draft; else started (and last saved).</summary>
    public IReadOnlyList<ApplicationEvent> EventsFor(WizardDraft draft, string? reference = null)
    {
        reference ??= draft.SourceReference ?? draft.Id;
        if (draft.SourceReference is not null)
            return store.Events().Where(e => e.ApplicationReference == draft.SourceReference)
                .Select(e => e with { ApplicationReference = reference }).OrderBy(e => e.At).ToList();

        var started = new ApplicationEvent(reference, draft.CreatedAt, ApplicationEventKind.Created, "Application started", "You",
            Status: ApplicationStatus.Draft);
        return draft.UpdatedAt - draft.CreatedAt > TimeSpan.FromMinutes(1)
            ? [started, new(reference, draft.UpdatedAt, ApplicationEventKind.DraftSaved, "Draft saved", "You")]
            : [started];
    }

    /// <summary>
    /// The seed checklist for a continued application while its program is unchanged; otherwise
    /// (a new application, or a resumed seed draft whose program changed) the chosen program's
    /// documents, all still to prepare.
    /// </summary>
    private ApplicationChecklist ChecklistFor(string reference, AssistanceProgram program, string patientName, string? sourceReference)
    {
        var seedApplication = sourceReference is null ? null
            : store.Applications().FirstOrDefault(a => a.ReferenceNumber == sourceReference);
        if (seedApplication?.ProgramId == program.Id
            && store.Checklists.FirstOrDefault(c => c.ApplicationReference == sourceReference) is { } seeded)
            return seeded with { ApplicationReference = reference, PatientName = patientName };

        return new ApplicationChecklist(reference, program.Name, patientName,
            program.RequiredDocuments.Select(d => new ChecklistItem(d, DocumentStatus.Missing, true, "See the program's requirements")).ToList());
    }

    /// <summary>Write the snapshot to the browser; raises <see cref="Changed"/> either way.</summary>
    private async Task<bool> PersistAsync()
    {
        var saved = await storage.SaveAsync(_snapshot);
        LastSaveFailed = !saved;
        Changed?.Invoke();
        return saved;
    }
}

/// <summary>The outcome of a simulated upload: saved (and whether it completed a correction), or why not.</summary>
public sealed record UploadResult(DocumentUpload? Upload, bool CorrectionSent, string? Error)
{
    public bool Succeeded => Upload is not null;

    public static UploadResult Saved(DocumentUpload upload, bool correctionSent) => new(upload, correctionSent, null);

    public static UploadResult Invalid(string error) => new(null, false, error);
}

/// <summary>The outcome of saving a patient profile: the saved profile, or field errors / a save failure.</summary>
public sealed record ProfileResult(PatientProfile? Profile, IReadOnlyDictionary<string, string> Errors, string? SaveError)
{
    public bool Succeeded => Profile is not null;

    public static ProfileResult Saved(PatientProfile profile) => new(profile, new Dictionary<string, string>(), null);

    public static ProfileResult Failed(Dictionary<string, string> errors) => new(null, errors, null);

    public static ProfileResult SaveFailed() =>
        new(null, new Dictionary<string, string>(), "We couldn’t save this profile in this browser. Try again.");
}

/// <summary>The outcome of an authorization request: the saved request, or field errors / a save failure.</summary>
public sealed record AuthorizationResult(AuthorizationRequest? Request, IReadOnlyDictionary<string, string> Errors, string? SaveError)
{
    public bool Succeeded => Request is not null;

    public static AuthorizationResult Saved(AuthorizationRequest request) => new(request, new Dictionary<string, string>(), null);

    public static AuthorizationResult Failed(Dictionary<string, string> errors) => new(null, errors, null);

    public static AuthorizationResult SaveFailed() =>
        new(null, new Dictionary<string, string>(), "We couldn’t save this request in this browser. Try again.");
}
