using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// One user's view of their applications. Drafts (and uploads to drafts), patient profiles, read
/// states and account changes are theirs alone: per
/// circuit (scoped), loaded from their browser storage once the page is interactive and written
/// back on every change. Submitted applications, their uploads, authorization and runner requests
/// and the notifications about them live in the shared <see cref="WorkflowService"/> (D9a, D9b) so
/// administrators can act on them; this class
/// shows the household's part of it and passes submissions and uploads through.
/// <see cref="Changed"/> tells pages to re-query (also when a reviewer changes something).
/// <para>
/// The seeded <see cref="MockPortalStore"/> stays read-only; a draft or shared copy with the same
/// reference takes its place in the merged view.
/// </para>
/// </summary>
public sealed class ApplicationWorkspace(IWorkspaceStorage storage, MockPortalStore store, WorkflowService workflow, TimeProvider clock,
    ICurrentActor currentActor)
    : IDisposable
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

    /// <summary>The household's applications in the shared store (submitted or reviewed), newest first.</summary>
    public IReadOnlyList<LocalApplication> Submitted =>
    [
        .. workflow.State.Applications.Values
            .Where(a => IsHousehold(a.Application.PatientId))
            .OrderByDescending(a => a.Application.LastUpdated)
            .Select(a => a.AsLocal()),
    ];

    /// <summary>
    /// This browser's notifications and the shared ones about this family's patients (M3.2). A
    /// shared notification names its patient; an older one without a patient is shown only when it
    /// links to one of this family's applications.
    /// </summary>
    public IReadOnlyList<PortalNotification> Notifications =>
        [.. _snapshot.Notifications, .. workflow.State.Notifications.Where(IsForThisFamily)];

    private bool IsForThisFamily(PortalNotification n)
    {
        if (n.PatientId is { } patientId) return IsHousehold(patientId);
        const string prefix = "/patient/applications/";
        return n.Href is { } href && href.StartsWith(prefix, StringComparison.Ordinal)
            && CurrentApplication(href[prefix.Length..]) is not null;
    }

    /// <summary>Uploads to drafts (this browser) and to the household's submitted applications (shared).</summary>
    public IReadOnlyList<DocumentUpload> Uploads =>
    [
        .. LocalUploads,
        .. workflow.State.Uploads.Where(u => workflow.Find(u.ApplicationReference) is { } a && IsHousehold(a.Application.PatientId)),
    ];

    /// <summary>Uploads kept only in this browser (to drafts).</summary>
    public int LocalUploadCount => LocalUploads.Count;

    private IReadOnlyList<DocumentUpload> LocalUploads => _snapshot.Uploads ?? [];
    private IReadOnlyList<PatientProfile> LocalProfiles => _snapshot.Profiles ?? [];
    private IReadOnlyList<NotificationReadState> NotificationStates => _snapshot.NotificationStates ?? [];

    /// <summary>Read the user's saved workspace once; later calls return the same task.</summary>
    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        _snapshot = await storage.LoadAsync() ?? WorkspaceSnapshot.Empty;
        IsLoaded = true;
        workflow.Changed += OnSharedChanged;
        Changed?.Invoke();
    }

    // A submission, upload or review decision (from any browser or the administrator) changed the shared store.
    private void OnSharedChanged() => Changed?.Invoke();

    public void Dispose() => workflow.Changed -= OnSharedChanged;

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
            return [.. seeded.Concat(added).Select(WithAuthorization)];
        }
    }

    /// <summary>A family member's authorization follows their latest request in the shared store (D9b).</summary>
    private PatientProfile WithAuthorization(PatientProfile profile) =>
        profile.Authorization != AuthorizationStatus.Self && workflow.LatestAuthorization(profile.Id) is { } latest
            ? profile with { Authorization = StaffDecisionRules.ProfileStatus(latest.Request.Status) }
            : profile;

    public bool IsHousehold(Guid patientId) => Household.Any(p => p.Id == patientId);

    /// <summary>A patient's authorization requests (seeded and shared), newest first.</summary>
    public IReadOnlyList<AuthorizationRequest> RequestsFor(Guid patientId) =>
        [.. workflow.AuthorizationsFor(patientId).Select(a => a.Request)];

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
        // The account holder's name belongs to the account: it changes in Settings (D8), which
        // updates this profile too, so the two never disagree.
        if (isSelf) fullName = existing!.FullName;
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
        var href = $"/patient/patients/{Navigation.ApplicationsHref.PatientKey(patient, Household)}";
        var notification = new PortalNotification($"n-auth-{request.Id}", "Authorization requested",
            $"Your request to act for {patient.FullName} is being reviewed.", now, false, href, patientId);

        // The request goes to the shared store so an administrator can decide; the profile's status follows it.
        var shared = new SharedAuthorization(request, patient.FullName, patient.Relationship, patient.DateOfBirth, href, 0);
        return await workflow.RequestAuthorizationAsync(await currentActor.GetAsync(), shared, notification)
            ? AuthorizationResult.Saved(request)
            : AuthorizationResult.SaveFailed();
    }

    // ---- Account details and saved data (D8) ----

    /// <summary>
    /// Give the "You" patient profile the account's name (M3.4b: the name lives on the sign-in
    /// account). Returns false when this browser couldn't save it (nothing changes then).
    /// </summary>
    public async Task<bool> SyncSelfNameAsync(string fullName)
    {
        var self = Household.FirstOrDefault(p => p.Authorization == AuthorizationStatus.Self);
        if (self is null || self.FullName == fullName || string.IsNullOrWhiteSpace(fullName)) return true;

        var before = _snapshot;
        _snapshot = _snapshot with { Profiles = [.. LocalProfiles.Where(p => p.Id != self.Id), self with { FullName = fullName }] };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Remove what this user saved in this browser (drafts, uploads to drafts, profiles, read
    /// states and account changes). Added patients with applications or requests in the shared
    /// store are kept (M3.2), so the family can still see and follow them. Returns false when it
    /// could not be saved.
    /// </summary>
    public async Task<bool> ClearAsync()
    {
        var before = _snapshot;
        // Keep added patients with shared records (M3.2) and the "You" profile's account name (M3.4b).
        var keep = LocalProfiles.Where(p => (store.Household.All(s => s.Id != p.Id) && workflow.HasRecordsFor(p.Id))
            || p.Authorization == AuthorizationStatus.Self).ToList();
        _snapshot = WorkspaceSnapshot.Empty with { Profiles = keep };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return false;
        }
        return true;
    }

    // ---- Notifications: read/unread and links (D7) ----

    /// <summary>
    /// A notification as this user sees it: their read/unread choice overrides its default, and a
    /// notification stored before D7 (no link) gets one from its id.
    /// </summary>
    public PortalNotification WithReadState(PortalNotification notification)
    {
        var state = NotificationStates.FirstOrDefault(s => s.Id == notification.Id);
        return notification with
        {
            IsRead = state?.IsRead ?? notification.IsRead,
            Href = notification.Href ?? FallbackHref(notification.Id),
        };
    }

    /// <summary>Links for notifications saved before D7, from their id ("n-MA-2026-00453", "n-auth-…", "n-rr-…").</summary>
    private static string? FallbackHref(string id) =>
        id.StartsWith("n-MA-", StringComparison.Ordinal) ? $"/patient/applications/{id[2..].Replace("-corrected", "")}"
        : id.StartsWith("n-auth-", StringComparison.Ordinal) ? "/patient/patients"
        : id.StartsWith("n-rr-", StringComparison.Ordinal) ? "/patient/runner"
        : null;

    /// <summary>Mark notifications read or unread. Returns false when the choice could not be saved (nothing changes).</summary>
    public async Task<bool> SetReadAsync(IEnumerable<string> ids, bool isRead)
    {
        var changed = ids.ToHashSet();
        if (changed.Count == 0) return true;

        var before = _snapshot;
        _snapshot = _snapshot with
        {
            NotificationStates = [.. NotificationStates.Where(s => !changed.Contains(s.Id)), .. changed.Select(id => new NotificationReadState(id, isRead))],
        };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return false;
        }
        return true;
    }

    // ---- Optional runner requests (D6) ----

    /// <summary>
    /// The household's runner requests from the shared store (D9b): the ones this account made
    /// (newest first), then seeded ones by date needed.
    /// </summary>
    public IReadOnlyList<RunnerRequestItem> RunnerRequests
    {
        get
        {
            var all = workflow.AllRunnerRequests().Where(r => CurrentApplication(r.Request.ApplicationReference) is not null).ToList();
            var mine = all.Where(r => workflow.IsFamilyRunnerRequest(r.Id)).Select(r => r.Request).OrderByDescending(r => r.RequestedAt);
            var seeded = all.Where(r => !workflow.IsFamilyRunnerRequest(r.Id)).Select(r => r.Request).OrderByDescending(r => r.NeededBy);
            return [.. mine, .. seeded];
        }
    }

    /// <summary>True for requests this account made (they can cancel one before it is assigned).</summary>
    public bool IsOwnRunnerRequest(string id) => workflow.IsFamilyRunnerRequest(id) && RunnerRequests.Any(r => r.Id == id);

    /// <summary>
    /// Ask for optional runner help with one errand. Validates at this boundary (eligible
    /// application, one open request per application, permitted errand, date within 30 days, time
    /// window, short notes, confirmation). The application's status does not change. Nothing is
    /// recorded unless it is saved.
    /// </summary>
    public async Task<RunnerRequestResult> RequestRunnerAsync(string? reference, RunnerErrand? errand, DateOnly? date,
        string? window, string? notes, bool confirmed)
    {
        var now = clock.GetLocalNow();
        var application = reference is null ? null : CurrentApplication(reference);
        var open = application is not null && RunnerRequests.Any(r =>
            Same(r.ApplicationReference, application.ReferenceNumber) && RunnerRules.IsOpen(r.Status));
        var errors = RunnerRules.Validate(application, open, errand, date, window, notes, confirmed, DateOnly.FromDateTime(now.Date));
        // No runner help while the patient's authorization stands rejected (M3.2).
        if (!errors.ContainsKey("application") && application is not null
            && workflow.Authorization(application.PatientId) == AuthorizationStanding.Rejected)
            errors["application"] = $"{application.PatientName}'s authorization wasn't approved, so runner help isn't available for this application.";
        if (errors.Count > 0) return RunnerRequestResult.Failed(errors);

        var hospital = store.Hospitals.FirstOrDefault(h => h.Name == application!.HospitalName);
        var startHour = window == RunnerRules.Windows[0] ? 8 : 13;
        var neededBy = new DateTimeOffset(date!.Value.ToDateTime(new TimeOnly(startHour, 0)), now.Offset);
        var request = new RunnerRequestItem(
            $"rr-{Guid.NewGuid().ToString("N")[..8]}", application!.ReferenceNumber,
            RunnerRules.ErrandLabel(errand!.Value),
            hospital is null ? application.HospitalName : $"{hospital.Name}, social service office",
            neededBy, RunnerTaskStatus.PendingAssignment, null,
            errand, window, string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), now);
        var notification = new PortalNotification($"n-{request.Id}", "Runner help requested",
            $"Your runner request for {application.ReferenceNumber} is waiting for a runner.", now, false, "/patient/runner",
            application.PatientId);

        return await workflow.RequestRunnerAsync(await currentActor.GetAsync(), request, notification)
            ? RunnerRequestResult.Saved(request)
            : RunnerRequestResult.SaveFailed();
    }

    /// <summary>Cancel one of this account's requests while it is still waiting for a runner. False if not allowed or not saved.</summary>
    public async Task<bool> CancelRunnerRequestAsync(string id) =>
        await workflow.CancelRunnerRequestAsync(await currentActor.GetAsync(), id, reference => CurrentApplication(reference) is not null);

    /// <summary>
    /// History entries for runner requests on an application: this account's requests and
    /// cancellations, and every runner step (assigned, accepted, on the way, done, handed back).
    /// Seeded requests already have their own "requested" entry in the seeded history. A runner's
    /// reason for handing back stays with the office.
    /// </summary>
    public IReadOnlyList<ApplicationEvent> RunnerEvents(string reference) =>
    [
        .. RunnerRequests.Where(r => Same(r.ApplicationReference, reference)).SelectMany(r =>
        {
            var own = workflow.IsFamilyRunnerRequest(r.Id);
            var events = new List<ApplicationEvent>();
            if (own)
                events.Add(new(reference, r.RequestedAt ?? r.NeededBy, ApplicationEventKind.RunnerRequested,
                    "Runner help requested", "You", $"{r.Errand} at {r.Place}."));
            if (r.Steps is { } steps)
                events.AddRange(steps.Select(s => RunnerStepEvent(reference, s)));
            else if (r.AssignedAt is { } assigned)
                events.Add(RunnerStepEvent(reference, new RunnerStep(assigned, RunnerStepKind.Assigned, r.RunnerName ?? "a runner")));
            if (own && r.Status == RunnerTaskStatus.Cancelled && r.ClosedAt is { } closed)
                events.Add(new(reference, closed, ApplicationEventKind.RunnerRequested, "Runner request cancelled", "You"));
            return events;
        }),
    ];

    private static ApplicationEvent RunnerStepEvent(string reference, RunnerStep step) => step.Kind switch
    {
        RunnerStepKind.Assigned => new(reference, step.At, ApplicationEventKind.RunnerRequested, $"Runner assigned: {step.Runner}", "MediAssist",
            "Runner help doesn't change the application's status."),
        RunnerStepKind.Accepted => new(reference, step.At, ApplicationEventKind.RunnerRequested, $"Runner accepted: {step.Runner}", "Runner"),
        RunnerStepKind.Started => new(reference, step.At, ApplicationEventKind.RunnerRequested, "Runner on the way", "Runner"),
        RunnerStepKind.Completed => new(reference, step.At, ApplicationEventKind.RunnerRequested, "Runner errand done", "Runner", step.Note),
        _ => new(reference, step.At, ApplicationEventKind.RunnerRequested, "Runner can't help", "MediAssist",
            "The office will assign another runner."),
    };

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
    /// Submission: the draft becomes a Submitted application in the shared store, with a reference
    /// number chosen there, a checklist (missing documents allowed) and its history; a notification
    /// confirms it; any documents uploaded to the draft move with it. The draft then leaves this
    /// browser. Returns the reference, or null when it could not be saved (nothing changes then).
    /// The caller validates the choices first.
    /// </summary>
    public async Task<string?> SubmitAsync(WizardDraft draft)
    {
        var now = clock.GetLocalNow();
        var patient = Household.First(p => p.Id == draft.PatientId);
        var program = store.Programs.First(p => p.Id == draft.ProgramId);
        var seed = draft.SourceReference is null ? null
            : store.Applications().FirstOrDefault(a => a.ReferenceNumber == draft.SourceReference);
        var draftKey = draft.SourceReference ?? draft.Id;
        var draftUploads = LocalUploads.Where(u => Same(u.ApplicationReference, draftKey)).ToList();

        var reference = await workflow.SubmitAsync(await currentActor.GetAsync(), draft.SourceReference, reference =>
        {
            var uploads = draftUploads.Select(u => u with { ApplicationReference = reference }).ToList();
            var checklist = ChecklistFor(reference, program, patient.FullName, draft.SourceReference);
            var withUploads = ChecklistOverlay.Apply(checklist, uploads);
            var ready = checklist.ReadyCount;
            var required = checklist.Items.Count;
            var toAdd = withUploads.Items.Count(i => DocumentStatusDisplay.NeedsAction(i.Status));

            var application = new AssistanceApplication(
                reference, patient.Id, patient.FullName, draft.Hospital!, program.Name,
                ApplicationStatus.Submitted, ready, required, now,
                toAdd > 0 ? WorkflowService.AddMissing(toAdd) : "Wait for the review to start",
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
                $"n-{reference}", "Application submitted", $"{reference} was sent for review.", now, false,
                $"/patient/applications/{reference}", patient.Id);
            return new Submission(new SharedApplication(application, checklist, history, 0), uploads, notification);
        });
        if (reference is null) return null;

        // The application is saved in the shared store; if this browser can't save, the draft
        // only reappears here on the next visit.
        _snapshot = _snapshot with
        {
            Drafts = _snapshot.Drafts.Where(d => d.Id != draft.Id).ToList(),
            Uploads = LocalUploads.Where(u => !Same(u.ApplicationReference, draftKey)).ToList(),
        };
        await PersistAsync();
        return reference;
    }

    // ---- Simulated uploads (D3) ----

    /// <summary>
    /// Record a simulated upload for one document. Uploads to a draft stay in this browser; uploads
    /// to a submitted application go to the shared store, which also returns a corrected
    /// application to review. Nothing is recorded unless it is saved.
    /// </summary>
    public async Task<UploadResult> UploadAsync(string reference, string documentName, SampleFile? file)
    {
        if (!IsDraftReference(reference))
            return await workflow.UploadAsync(await currentActor.GetAsync(), reference, documentName, file, IsHousehold);

        if (SimulatedFiles.Validate(file) is string invalid)
            return UploadResult.Invalid(invalid);

        var item = BaseChecklist(reference)?.Items.FirstOrDefault(i => i.Name == documentName);
        var previous = LatestUpload(reference, documentName);
        if (item is null || !(DocumentStatusDisplay.NeedsAction(item.Status) || previous is not null))
            return UploadResult.Invalid($"{documentName} cannot be uploaded for this application.");

        var upload = new DocumentUpload(
            Guid.NewGuid().ToString("N")[..12], reference, documentName,
            file!.FileName, file.ContentType, file.SizeBytes, clock.GetLocalNow(),
            (previous?.Version ?? 0) + 1, previous?.Id);

        var before = _snapshot;
        _snapshot = _snapshot with { Uploads = [.. LocalUploads, upload] };
        if (!await PersistAsync())
        {
            _snapshot = before;
            Changed?.Invoke();
            return UploadResult.Invalid("We couldn’t save that upload in this browser. Try again.");
        }
        return UploadResult.Saved(upload, false);
    }

    /// <summary>A draft (this browser's, or a seeded one not submitted yet) rather than a submitted application.</summary>
    private bool IsDraftReference(string reference) =>
        FindDraftByKey(reference) is not null
        || (!workflow.State.Applications.ContainsKey(reference) && CurrentApplication(reference)?.Status == ApplicationStatus.Draft);

    private WizardDraft? FindDraftByKey(string reference) =>
        _snapshot.Drafts.FirstOrDefault(d => Same(d.SourceReference ?? d.Id, reference));

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
    public ApplicationChecklist WithUploads(ApplicationChecklist checklist) => ChecklistOverlay.Apply(checklist, Uploads);

    /// <summary>History entries for the user's uploads to an application.</summary>
    public IReadOnlyList<ApplicationEvent> UploadEvents(string reference) =>
        WorkflowService.UploadEvents(reference, Uploads, BaseChecklist(reference));

    /// <summary>The household's application behind a reference (shared copy, draft or seed), before uploads.</summary>
    private AssistanceApplication? CurrentApplication(string reference)
    {
        if (workflow.State.Applications.TryGetValue(reference, out var shared))
            return IsHousehold(shared.Application.PatientId) ? shared.Application : null;
        if (FindDraftByKey(reference) is { } draft) return DraftAsApplication(draft);
        return workflow.Find(reference) is { } seeded && IsHousehold(seeded.Application.PatientId) ? seeded.Application : null;
    }

    /// <summary>The checklist behind a reference, before uploads.</summary>
    private ApplicationChecklist? BaseChecklist(string reference)
    {
        if (workflow.State.Applications.TryGetValue(reference, out var shared))
            return IsHousehold(shared.Application.PatientId) ? shared.Checklist : null;
        if (FindDraftByKey(reference) is { } draft) return DraftChecklistBase(draft);
        return CurrentApplication(reference) is null ? null : store.Checklists.FirstOrDefault(c => Same(c.ApplicationReference, reference));
    }

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

/// <summary>The outcome of saving the account form (D8): saved, field errors, or a save failure.</summary>
public sealed record AccountResult(AccountDetails? Account, IReadOnlyDictionary<string, string> Errors, string? SaveError)
{
    public bool Succeeded => Account is not null;

    public static AccountResult Saved(AccountDetails account) => new(account, new Dictionary<string, string>(), null);

    public static AccountResult Failed(Dictionary<string, string> errors) => new(null, errors, null);

    public static AccountResult SaveFailed() =>
        new(null, new Dictionary<string, string>(), "We couldn’t save your details in this browser. Try again.");
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
        new(null, new Dictionary<string, string>(), "We couldn’t save this request. It may already have been sent from another browser; reload and check.");
}

/// <summary>The outcome of a runner request: the saved request, or field errors / a save failure.</summary>
public sealed record RunnerRequestResult(RunnerRequestItem? Request, IReadOnlyDictionary<string, string> Errors, string? SaveError)
{
    public bool Succeeded => Request is not null;

    public static RunnerRequestResult Saved(RunnerRequestItem request) => new(request, new Dictionary<string, string>(), null);

    public static RunnerRequestResult Failed(Dictionary<string, string> errors) => new(null, errors, null);

    public static RunnerRequestResult SaveFailed() =>
        new(null, new Dictionary<string, string>(), "We couldn’t save this request. The application may already have one in progress; reload and check.");
}
