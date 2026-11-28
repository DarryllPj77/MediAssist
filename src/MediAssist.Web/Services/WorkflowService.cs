using System.Text.Json;
using MediAssist.Web.Data;
using MediAssist.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MediAssist.Web.Services;

/// <summary>
/// The shared workflow (D9a, D9b): submitted applications, their uploads and review decisions,
/// authorization requests and runner requests, and the notifications they send, kept in SQLite so
/// the patient and administrator portals see one record. A singleton: <see cref="State"/> is an immutable read model loaded at
/// start-up and swapped whole after each saved change, so reads are synchronous and safe from any
/// circuit. Every change goes through one gate, checks its rules there, writes to the database
/// and only then updates the read model and raises <see cref="Changed"/>.
/// <para>
/// Seeded applications stay in <see cref="MockPortalStore"/> (with day-relative dates) until the
/// first change, which copies the application into the shared store at version 1.
/// </para>
/// </summary>
public sealed class WorkflowService(
    IDbContextFactory<WorkflowDbContext> factory,
    MockPortalStore store,
    TimeProvider clock,
    ILogger<WorkflowService> logger,
    WorkflowOptions? options = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Reviewer = "Reviewer";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private WorkflowState _state = WorkflowState.Empty;
    private readonly WorkflowOptions _options = options ?? new();
    private Views? _views;

    /// <summary>Raised after a saved change, on the thread that made it. Handlers marshal to their renderer.</summary>
    public event Action? Changed;

    public WorkflowState State => Volatile.Read(ref _state);

    /// <summary>Create the database if needed and load the read model. Called once at start-up.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await BringUnderMigrationsAsync(db, cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await ApplyRetentionAsync(db, cancellationToken);
        await LoadStateAsync(db, cancellationToken);
    }

    /// <summary>
    /// Retention (M3.7), applied at start-up: notifications older than <see cref="WorkflowOptions.NotificationDays"/>
    /// and audit entries older than <see cref="WorkflowOptions.AuditDays"/> are deleted. Applications,
    /// uploads and requests are records and are kept.
    /// </summary>
    private async Task ApplyRetentionAsync(WorkflowDbContext db, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var auditCutoff = now.UtcDateTime.AddDays(-_options.AuditDays);
        var audits = await db.AuditEntries.Where(e => e.AtUtc < auditCutoff).ExecuteDeleteAsync(cancellationToken);

        var noticeCutoff = now.AddDays(-_options.NotificationDays);
        var old = new List<string>();
        foreach (var row in await db.Notifications.AsNoTracking().ToListAsync(cancellationToken))
        {
            try
            {
                if (JsonSerializer.Deserialize<PortalNotification>(row.Json, Json) is { } n && n.CreatedAt < noticeCutoff) old.Add(row.Id);
            }
            catch (JsonException) { } // left for the tolerant loader to report
        }
        var notices = old.Count == 0 ? 0 : await db.Notifications.Where(n => old.Contains(n.Id)).ExecuteDeleteAsync(cancellationToken);
        if (audits + notices > 0)
            logger.LogInformation("Retention removed {Notifications} notifications and {AuditEntries} audit entries.", notices, audits);
    }

    /// <summary>
    /// A database made before M3.6 (by <c>EnsureCreated</c>, so with tables but no migration history)
    /// is marked as already at the first migration, after adding any tables a D9a-era file lacks;
    /// later migrations then apply normally. New or migrated databases are left alone.
    /// </summary>
    private static async Task BringUnderMigrationsAsync(WorkflowDbContext db, CancellationToken cancellationToken)
    {
        async Task<bool> TableExistsAsync(string name) =>
            await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {name}")
                .SingleAsync(cancellationToken) > 0;

        if (!await TableExistsAsync("Applications") || await TableExistsAsync("__EFMigrationsHistory")) return;

        var baseline = db.Database.GetMigrations().First();
        await db.Database.ExecuteSqlRawAsync(WorkflowDbContext.AddLaterTables, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);",
            cancellationToken);
        await db.Database.ExecuteSqlAsync($"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({baseline}, '10.0.15');",
            cancellationToken);
    }

    /// <summary>Read every row into a new read model. A row that can't be read is logged and skipped, so one bad row can't stop the app (M3.6).</summary>
    private async Task LoadStateAsync(WorkflowDbContext db, CancellationToken cancellationToken = default)
    {
        IEnumerable<T> Read<T>(IEnumerable<(string Key, string Json)> rows, string table) => rows.Select(r =>
        {
            try { return JsonSerializer.Deserialize<T>(r.Json, Json); }
            catch (JsonException ex) { logger.LogWarning(ex, "Skipped unreadable row {Key} in {Table}.", r.Key, table); return default; }
        }).OfType<T>();

        var applications = Read<SharedApplication>(
                (await db.Applications.AsNoTracking().ToListAsync(cancellationToken)).Select(r => (r.Reference, r.Json)), "Applications")
            .ToDictionary(a => a.Reference, StringComparer.OrdinalIgnoreCase);
        var uploads = Read<DocumentUpload>(
            (await db.Uploads.AsNoTracking().ToListAsync(cancellationToken)).Select(r => (r.Id, r.Json)), "Uploads").ToList();
        var notifications = Read<PortalNotification>(
                (await db.Notifications.AsNoTracking().ToListAsync(cancellationToken)).Select(r => (r.Id, r.Json)), "Notifications")
            .OrderByDescending(n => n.CreatedAt).ToList();
        var authorizations = Read<SharedAuthorization>(
            (await db.Authorizations.AsNoTracking().ToListAsync(cancellationToken)).Select(r => (r.Id, r.Json)), "Authorizations").ToDictionary(a => a.Id);
        var runnerRequests = Read<SharedRunnerRequest>(
            (await db.RunnerRequests.AsNoTracking().ToListAsync(cancellationToken)).Select(r => (r.Id, r.Json)), "RunnerRequests").ToDictionary(a => a.Id);

        Volatile.Write(ref _state, new WorkflowState(applications, uploads, notifications, authorizations, runnerRequests));
    }

    // ---- Reading ----

    /// <summary>The application behind a reference: the shared copy, else the seeded one (version 0).</summary>
    public SharedApplication? Find(string reference)
    {
        var views = Current;
        return views.State.Applications.TryGetValue(reference, out var shared) ? shared : views.Seeds.GetValueOrDefault(reference);
    }

    /// <summary>Every application (seeded and submitted, all accounts), shared copies replacing seeds.</summary>
    public IReadOnlyList<SharedApplication> All() => Current.All;

    /// <summary>
    /// Lists derived from one read model (M3.7): seeded copies, every application, authorization and
    /// runner request, and each patient's latest authorization. Built once per read model (and per
    /// day, since seeds have day-relative dates) instead of on every call; pages ask for them often.
    /// </summary>
    private sealed record Views(
        WorkflowState State, DateTimeOffset Day, string? SeedOwner,
        IReadOnlyDictionary<string, SharedApplication> Seeds, IReadOnlyList<SharedApplication> All,
        IReadOnlyList<SharedAuthorization> Authorizations, IReadOnlyDictionary<Guid, SharedAuthorization> Latest,
        IReadOnlyList<SharedRunnerRequest> RunnerRequests);

    private Views Current
    {
        get
        {
            var state = State;
            var day = store.Today;
            var owner = store.SeedOwnerId;
            var views = Volatile.Read(ref _views);
            if (views is not null && ReferenceEquals(views.State, state) && views.Day == day && views.SeedOwner == owner) return views;
            views = BuildViews(state, day, owner);
            Volatile.Write(ref _views, views);
            return views;
        }
    }

    private Views BuildViews(WorkflowState state, DateTimeOffset day, string? owner)
    {
        var events = store.Events().ToLookup(e => e.ApplicationReference, StringComparer.OrdinalIgnoreCase);
        var seeds = new Dictionary<string, SharedApplication>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in store.Applications().Where(a => !state.Applications.ContainsKey(a.ReferenceNumber)))
            seeds[a.ReferenceNumber] = new SharedApplication(a,
                store.Checklists.FirstOrDefault(c => Same(c.ApplicationReference, a.ReferenceNumber)) ?? GeneratedChecklist(a),
                [.. events[a.ReferenceNumber].OrderBy(e => e.At)], 0, store.SeedOwnerOf(a.PatientId));
        SharedApplication? find(string r) => state.Applications.TryGetValue(r, out var s) ? s : seeds.GetValueOrDefault(r);

        var authorizations = state.Authorizations.Values
            .Concat(store.AuthorizationRequests().Where(r => !state.Authorizations.ContainsKey(r.Id)).Select(SeededAuthorization))
            .OrderByDescending(a => a.Request.RequestedAt).ToList();
        var latest = new Dictionary<Guid, SharedAuthorization>();
        foreach (var a in authorizations) latest.TryAdd(a.Request.PatientId, a);
        var runners = state.RunnerRequests.Values
            .Concat(store.RunnerRequests().Where(r => !state.RunnerRequests.ContainsKey(r.Id))
                .Select(r => new SharedRunnerRequest(r, 0, find(r.ApplicationReference)?.OwnerId)))
            .ToList();
        return new Views(state, day, owner, seeds, [.. state.Applications.Values, .. seeds.Values], authorizations, latest, runners);
    }

    /// <summary>The checklist with undecided uploads shown as "being reviewed".</summary>
    public ApplicationChecklist Effective(SharedApplication application) =>
        ChecklistOverlay.Apply(application.Checklist, State.Uploads);

    public DocumentUpload? LatestUpload(string reference, string documentName) => Latest(State.Uploads, reference, documentName);

    /// <summary>True while the patient's latest authorization request waits for an administrator.</summary>
    public bool AuthorizationPending(Guid patientId) => Authorization(patientId) == AuthorizationStanding.Pending;

    /// <summary>
    /// Whether the account may act for this patient (M3.2): the latest request decides. A patient
    /// with no request needs none (the account holder, or a seeded patient of another account),
    /// since a family member can't apply without asking first.
    /// </summary>
    public AuthorizationStanding Authorization(Guid patientId) => LatestAuthorization(patientId)?.Request.Status switch
    {
        null => AuthorizationStanding.NotNeeded,
        AuthorizationRequestStatus.Approved => AuthorizationStanding.Approved,
        AuthorizationRequestStatus.Rejected => AuthorizationStanding.Rejected,
        _ => AuthorizationStanding.Pending
    };

    /// <summary>True when the patient has any shared application or authorization request (so a family must keep their profile).</summary>
    public bool HasRecordsFor(Guid patientId) =>
        State.Applications.Values.Any(a => a.Application.PatientId == patientId)
        || State.Authorizations.Values.Any(a => a.Request.PatientId == patientId);

    /// <summary>The application's history with its uploads, oldest first (the administrator's view).</summary>
    public IReadOnlyList<ApplicationEvent> History(SharedApplication application) =>
        [.. application.Events.Concat(UploadEvents(application.Reference, State.Uploads, application.Checklist)).OrderBy(e => e.At)];

    /// <summary>History entries for uploads to one application ("… uploaded", "… replaced (version 2)").</summary>
    public static IReadOnlyList<ApplicationEvent> UploadEvents(string reference, IEnumerable<DocumentUpload> uploads, ApplicationChecklist? baseChecklist)
    {
        var baseItems = baseChecklist?.Items.ToDictionary(i => i.Name, i => i.Status) ?? [];
        return uploads.Where(u => Same(u.ApplicationReference, reference)).Select(u =>
        {
            var title = u.Version > 1 ? $"{u.DocumentName} replaced (version {u.Version})"
                : baseItems.GetValueOrDefault(u.DocumentName) == DocumentStatus.NeedsCorrection ? $"Corrected {Lower(u.DocumentName)} uploaded"
                : $"{u.DocumentName} uploaded";
            return new ApplicationEvent(reference, u.UploadedAt, ApplicationEventKind.DocumentUploaded, title, "You",
                $"{u.FileName} · {SimulatedFiles.FormatSize(u.SizeBytes)}");
        }).ToList();
    }

    // ---- Patient actions ----

    /// <summary>
    /// Save a submission. The reference is chosen here, under the gate, so two browsers can't take
    /// the same one; a resumed seeded draft keeps its own. Returns the reference, or null when it
    /// could not be saved (or that seeded draft was already submitted elsewhere).
    /// </summary>
    public async Task<string?> SubmitAsync(Actor actor, string? fixedReference, Func<string, Submission> build)
    {
        if (actor.Problem(Data.Roles.Patient) is not null) return null;
        string? reference = null;
        var saved = false;
        await _gate.WaitAsync();
        try
        {
            var state = State;
            SharedApplication? existing = null;
            if (fixedReference is not null)
            {
                if (state.Applications.TryGetValue(fixedReference, out existing) && existing.Application.Status != ApplicationStatus.Draft)
                    return null;
                if (Find(fixedReference) is { OwnerId: var owner } && owner != actor.UserId) return null; // another account's draft
                reference = fixedReference;
            }
            else
            {
                reference = ReferenceGenerator.NextReference(
                    store.Applications().Select(a => a.ReferenceNumber).Concat(state.Applications.Keys), clock.GetLocalNow());
            }

            var submission = build(reference);
            var application = submission.Application with
            {
                OwnerId = actor.UserId, // the submitting account owns it (M3.5)
                Version = (existing?.Version ?? 0) + 1,
                // The family's own entries record which account made them (M3.4b).
                Events = [.. submission.Application.Events.Select(e => e.Actor == "You" && e.ActorId is null ? Stamp(e, actor) : e)],
            };
            saved = await CommitAsync(state, new Writes([application], submission.Uploads, [submission.Notification with { OwnerId = actor.UserId }],
                Audit: new(actor, "Submitted application", reference)));
        }
        finally
        {
            _gate.Release();
        }
        if (saved) Changed?.Invoke();
        return saved ? reference : null;
    }

    /// <summary>
    /// Record a simulated upload for a submitted application in the account's scope. A missing or
    /// sent-back document takes a copy; a copy still waiting for review can be replaced; an accepted
    /// one can't. When every sent-back document has a new copy, the application returns to review.
    /// </summary>
    public async Task<UploadResult> UploadAsync(Actor actor, string reference, string documentName, SampleFile? file)
    {
        if (actor.Problem(Data.Roles.Patient) is string notAllowed) return UploadResult.Invalid(notAllowed);
        if (SimulatedFiles.Validate(file) is string invalid) return UploadResult.Invalid(invalid);

        UploadResult result;
        await _gate.WaitAsync();
        try
        {
            var state = State;
            var now = clock.GetLocalNow();
            var current = Find(reference);
            var item = current?.Checklist.Items.FirstOrDefault(i => i.Name == documentName);
            var previous = Latest(state.Uploads, reference, documentName);
            var allowed = current is not null && current.OwnerId == actor.UserId && item is not null
                && current.Application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Completed or ApplicationStatus.Cancelled)
                && (DocumentStatusDisplay.NeedsAction(item.Status)
                    || (item.Status == DocumentStatus.PendingReview && previous?.ReviewState == UploadReviewState.Pending));
            if (!allowed) return UploadResult.Invalid($"{documentName} cannot be uploaded for this application.");

            var upload = new DocumentUpload(
                Guid.NewGuid().ToString("N")[..12], current!.Reference, documentName,
                file!.FileName, file.ContentType, file.SizeBytes, now,
                (previous?.Version ?? 0) + 1, previous?.Id);
            IReadOnlyList<DocumentUpload> uploads = [.. state.Uploads, upload];

            var application = current.Application with { LastUpdated = now };
            var events = current.Events;
            var notifications = new List<PortalNotification>();
            var stillMissing = ChecklistOverlay.Apply(current.Checklist, uploads).Items.Count(i => DocumentStatusDisplay.NeedsAction(i.Status));
            var corrected = application.Status == ApplicationStatus.NeedsCorrection
                && current.Checklist.Items.Where(i => i.Status == DocumentStatus.NeedsCorrection)
                    .All(i => Latest(uploads, reference, i.Name)?.ReviewState == UploadReviewState.Pending);

            if (corrected)
            {
                // Phase 1 rule: a corrected application goes back to review.
                application = application with
                {
                    Status = ApplicationStatus.UnderReview,
                    NextStep = stillMissing > 0 ? AddMissing(stillMissing) : "Wait for the reviewer to check the corrected document",
                };
                events = [.. events, Stamp(new(current.Reference, now, ApplicationEventKind.CorrectionSent, "Correction sent for review", "You",
                    "Every document the reviewer asked for has a new copy, so the application is back in review.",
                    ApplicationStatus.UnderReview), actor)];
                notifications.Add(Notice(current, "corrected", "Correction sent",
                    $"{current.Reference} is back in review with your corrected document.", now));
            }
            else if (application.Status is ApplicationStatus.Submitted or ApplicationStatus.UnderReview)
            {
                application = application with
                {
                    NextStep = stillMissing > 0 ? AddMissing(stillMissing)
                        : application.Status == ApplicationStatus.Submitted ? "Wait for the review to start"
                        : "Wait for the document review to finish",
                };
            }

            // A copy keeps the owner (and anything added later); only these fields change.
            var updated = current with { Application = application, Events = events, Version = current.Version + 1 };
            result = await CommitAsync(state, new Writes([updated], [upload], notifications, Audit: new(actor, $"Uploaded {documentName}", reference)))
                ? UploadResult.Saved(upload, corrected)
                : UploadResult.Invalid("We couldn’t save that upload. Try again.");
        }
        finally
        {
            _gate.Release();
        }
        if (result.Succeeded) Changed?.Invoke();
        return result;
    }

    // ---- Administrator actions (each checks the version the reviewer saw) ----

    public Task<ReviewResult> StartReviewAsync(Actor actor, string reference, int expectedVersion) =>
        ActAsync(actor, reference, expectedVersion, (current, _, now) =>
        {
            if (ReviewRules.StartProblem(current.Application.Status, Authorization(current.Application.PatientId)) is string problem)
                return Change.Refuse(problem);
            var application = current.Application with
            {
                Status = ApplicationStatus.UnderReview, LastUpdated = now, NextStep = "Wait for the document review to finish",
            };
            return Change.To(current with
            {
                Application = application,
                Events = [.. current.Events, new(current.Reference, now, ApplicationEventKind.ReviewStarted, "Review started", Reviewer,
                    Status: ApplicationStatus.UnderReview)],
            }, notification: Notice(current, "review", "Review started", $"{current.Reference} is now under review.", now));
        });

    /// <summary>Accept a document, or send it back (or ask for a missing one) with a reason the family will see.</summary>
    public Task<ReviewResult> DecideDocumentAsync(Actor actor, string reference, int expectedVersion, string documentName, bool accept, string? reason) =>
        ActAsync(actor, reference, expectedVersion, (current, effective, now) =>
        {
            var item = effective.Items.FirstOrDefault(i => i.Name == documentName);
            if (ReviewRules.DecideProblem(current.Application.Status, item, accept) is string problem) return Change.Refuse(problem);
            if (!accept && ReviewRules.ReasonProblem(reason) is string reasonProblem) return Change.Refuse(reasonProblem);

            var note = accept ? null : reason!.Trim();
            var decided = item! with { Status = accept ? DocumentStatus.Complete : DocumentStatus.NeedsCorrection, ReviewerNote = note };
            var checklist = current.Checklist with
            {
                Items = current.Checklist.Items.Select(i => i.Name == documentName ? decided : i).ToList(),
            };
            var latest = LatestUpload(reference, documentName);
            DocumentUpload[] uploads = latest?.ReviewState == UploadReviewState.Pending
                ? [latest with { ReviewState = accept ? UploadReviewState.Accepted : UploadReviewState.Rejected, ReviewNote = note }]
                : [];
            var title = accept ? $"{documentName} accepted"
                : item.Status == DocumentStatus.Missing ? $"{documentName} requested" : $"{documentName} needs a new copy";

            return Change.To(current with
            {
                Application = current.Application with { DocumentsReady = checklist.ReadyCount, LastUpdated = now },
                Checklist = checklist,
                Events = [.. current.Events, new(current.Reference, now,
                    accept ? ApplicationEventKind.DocumentAccepted : ApplicationEventKind.DocumentRejected, title, Reviewer, note)],
            }, uploads);
        });

    /// <summary>Send the application back to the family with the documents marked for correction.</summary>
    public Task<ReviewResult> RequestCorrectionsAsync(Actor actor, string reference, int expectedVersion) =>
        ActAsync(actor, reference, expectedVersion, (current, effective, now) =>
        {
            if (ReviewRules.CorrectionProblem(current.Application.Status, effective) is string problem) return Change.Refuse(problem);
            var flagged = effective.Items.Where(i => i.Status == DocumentStatus.NeedsCorrection).ToList();
            var next = flagged.Count == 1 ? $"Send a new copy of the {Lower(flagged[0].Name)}" : $"Send new copies of {flagged.Count} documents";
            var detail = string.Join(" ", flagged.Select(i => $"{i.Name}: {i.ReviewerNote}"));
            return Change.To(current with
            {
                Application = current.Application with { Status = ApplicationStatus.NeedsCorrection, LastUpdated = now, NextStep = next },
                Events = [.. current.Events, new(current.Reference, now, ApplicationEventKind.CorrectionRequested, "Correction requested",
                    Reviewer, detail, ApplicationStatus.NeedsCorrection)],
            }, notification: Notice(current, "correction", "Correction requested",
                flagged.Count == 1 ? $"{current.Reference} needs a new copy of the {Lower(flagged[0].Name)}."
                    : $"{current.Reference} needs new copies of {flagged.Count} documents.", now));
        });

    public Task<ReviewResult> MarkReadyAsync(Actor actor, string reference, int expectedVersion) =>
        ActAsync(actor, reference, expectedVersion, (current, effective, now) =>
        {
            if (ReviewRules.ReadyProblem(current.Application.Status, effective) is string problem) return Change.Refuse(problem);
            return Change.To(current with
            {
                Application = current.Application with
                {
                    Status = ApplicationStatus.ReadyForProcessing, LastUpdated = now, NextStep = "No action needed from you",
                },
                Events = [.. current.Events, new(current.Reference, now, ApplicationEventKind.ReadyForProcessing, "Review finished",
                    Reviewer, "All documents were accepted. The program office will process the application.",
                    ApplicationStatus.ReadyForProcessing)],
            }, notification: Notice(current, "ready", "Review finished",
                $"{current.Reference} passed review and is with the program office.", now));
        });

    /// <summary>Internal processing is finished. This is not a financial decision; the program office makes that.</summary>
    public Task<ReviewResult> CompleteAsync(Actor actor, string reference, int expectedVersion) =>
        ActAsync(actor, reference, expectedVersion, (current, _, now) =>
        {
            if (ReviewRules.CompleteProblem(current.Application.Status) is string problem) return Change.Refuse(problem);
            return Change.To(current with
            {
                Application = current.Application with
                {
                    Status = ApplicationStatus.Completed, LastUpdated = now, NextStep = "Internal processing finished",
                },
                Events = [.. current.Events, new(current.Reference, now, ApplicationEventKind.Completed, "Application completed", "MediAssist",
                    "Internal processing is finished. The program office decides on any assistance and contacts the family.",
                    ApplicationStatus.Completed)],
            }, notification: Notice(current, "completed", "Application completed",
                $"Internal processing for {current.Reference} is finished.", now));
        });

    private sealed record Change(string? Error, SharedApplication? Updated, IReadOnlyList<DocumentUpload> Uploads, PortalNotification? Notification)
    {
        public static Change Refuse(string error) => new(error, null, [], null);

        public static Change To(SharedApplication updated, IReadOnlyList<DocumentUpload>? uploads = null, PortalNotification? notification = null) =>
            new(null, updated, uploads ?? [], notification);
    }

    private async Task<ReviewResult> ActAsync(Actor actor, string reference, int expectedVersion,
        Func<SharedApplication, ApplicationChecklist, DateTimeOffset, Change> decide)
    {
        if (actor.Problem(Data.Roles.Administrator) is string notAllowed) return ReviewResult.Refused(notAllowed);
        ReviewResult result;
        await _gate.WaitAsync();
        try
        {
            var current = Find(reference);
            if (current is null) return ReviewResult.Refused("That application doesn't exist.");
            if (current.Version != expectedVersion) return ReviewResult.Stale;

            var now = clock.GetLocalNow();
            var change = decide(current, Effective(current), now);
            if (change.Error is not null) return ReviewResult.Refused(change.Error);

            // The reviewer's new history entries record who made them (staff see the name; families see "Reviewer").
            var updated = change.Updated! with
            {
                Version = current.Version + 1,
                Events = [.. current.Events, .. change.Updated.Events.Skip(current.Events.Count).Select(e => Stamp(e, actor))],
            };
            var notify = change.Notification is not null && current.OwnerId is not null;
            // When the application ends, its open runner requests end with it (M3.7): no errand outlives it.
            SharedRunnerRequest[] closed = updated.Application.Status is ApplicationStatus.Completed or ApplicationStatus.Cancelled
                ? [.. AllRunnerRequests().Where(r => Same(r.Request.ApplicationReference, reference) && RunnerRules.IsOpen(r.Request.Status))
                    .Select(r => r with { Request = r.Request with { Status = RunnerTaskStatus.Cancelled, ClosedAt = now }, Version = r.Version + 1 })]
                : [];
            result = await CommitAsync(State, new Writes([updated], change.Uploads, notify ? [change.Notification! with { OwnerId = current.OwnerId }] : [],
                    RunnerRequests: closed, Audit: new(actor, updated.Events[^1].Title, reference)))
                ? ReviewResult.Done
                : ReviewResult.SaveFailed;
        }
        finally
        {
            _gate.Release();
        }
        if (result.Succeeded) Changed?.Invoke();
        return result;
    }

    // ---- Authorization requests (D9b) ----

    /// <summary>Every authorization request (seeded and shared, all accounts), shared copies replacing seeds, newest first.</summary>
    public IReadOnlyList<SharedAuthorization> AllAuthorizations() => Current.Authorizations;

    public SharedAuthorization? FindAuthorization(string id) =>
        State.Authorizations.TryGetValue(id, out var shared) ? shared
        : store.AuthorizationRequests().FirstOrDefault(r => r.Id == id) is { } seed ? SeededAuthorization(seed) : null;

    /// <summary>A patient's requests, newest first.</summary>
    public IReadOnlyList<SharedAuthorization> AuthorizationsFor(Guid patientId) =>
        [.. AllAuthorizations().Where(a => a.Request.PatientId == patientId)];

    /// <summary>The patient's most recent request, which decides what their profile shows.</summary>
    public SharedAuthorization? LatestAuthorization(Guid patientId) => Current.Latest.GetValueOrDefault(patientId);

    private SharedAuthorization SeededAuthorization(AuthorizationRequest request)
    {
        var patient = store.Household.FirstOrDefault(p => p.Id == request.PatientId);
        return new SharedAuthorization(request, patient?.FullName ?? "Patient", patient?.Relationship ?? "",
            patient?.DateOfBirth,
            patient is null ? "/patient/patients" : $"/patient/patients/{Navigation.ApplicationsHref.PatientKey(patient, store.Household)}", 0,
            OwnerId: store.SeedOwnerOf(request.PatientId));
    }

    /// <summary>
    /// Save a family's request. Refused while the patient already has one waiting or approved, so
    /// two browsers can't both ask. The family's own checks (basis, file, confirmation) run first.
    /// </summary>
    public async Task<bool> RequestAuthorizationAsync(Actor actor, SharedAuthorization authorization, PortalNotification notification)
    {
        if (actor.Problem(Data.Roles.Patient) is not null) return false;
        bool saved;
        await _gate.WaitAsync();
        try
        {
            var latest = LatestAuthorization(authorization.Request.PatientId);
            if (latest?.Request.Status is AuthorizationRequestStatus.Pending or AuthorizationRequestStatus.Approved) return false;
            if (latest is { OwnerId: var owner } && owner != actor.UserId) return false; // another account's patient
            saved = await CommitAsync(State, new Writes(Notifications: [notification with { OwnerId = actor.UserId }],
                Audit: new(actor, "Requested authorization", authorization.Id), Authorizations: [authorization with { Version = 1, OwnerId = actor.UserId }]));
        }
        finally
        {
            _gate.Release();
        }
        if (saved) Changed?.Invoke();
        return saved;
    }

    /// <summary>Approve, or don't approve with a note the family sees. The family's profile follows.</summary>
    public async Task<ReviewResult> DecideAuthorizationAsync(Actor actor, string id, int expectedVersion, bool approve, string? note)
    {
        if (actor.Problem(Data.Roles.Administrator) is string notAllowed) return ReviewResult.Refused(notAllowed);
        ReviewResult result;
        await _gate.WaitAsync();
        try
        {
            var current = FindAuthorization(id);
            if (current is null) return ReviewResult.Refused("That request doesn't exist.");
            if (current.Version != expectedVersion) return ReviewResult.Stale;

            var now = clock.GetLocalNow();
            var request = current.Request;
            var problem = approve
                ? StaffDecisionRules.ApproveProblem(request.Status, request.Basis, current.DateOfBirth, DateOnly.FromDateTime(now.Date))
                : StaffDecisionRules.RejectProblem(request.Status, note);
            if (problem is not null) return ReviewResult.Refused(problem);

            var decided = current with
            {
                Request = request with
                {
                    Status = approve ? AuthorizationRequestStatus.Approved : AuthorizationRequestStatus.Rejected,
                    DecidedAt = now,
                    ReviewerNote = approve ? "The supporting document matches the request." : note!.Trim(),
                },
                Version = current.Version + 1,
                DecidedById = actor.UserId,
                DecidedByName = actor.Name,
            };
            var notice = new PortalNotification($"n-auth-{id}-{(approve ? "approved" : "rejected")}-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
                approve ? "Authorization approved" : "Authorization not approved",
                approve ? $"You can now apply and follow applications for {current.PatientName}."
                        : $"Your request to act for {current.PatientName} wasn't approved. See the reviewer's note.",
                now, false, current.PatientHref, current.Request.PatientId, current.OwnerId);
            result = await CommitAsync(State, new Writes(Notifications: current.OwnerId is null ? [] : [notice], Authorizations: [decided],
                Audit: new(actor, approve ? "Approved authorization" : "Rejected authorization", id)))
                ? ReviewResult.Done
                : ReviewResult.SaveFailed;
        }
        finally
        {
            _gate.Release();
        }
        if (result.Succeeded) Changed?.Invoke();
        return result;
    }

    // ---- Runner requests (D9b) ----

    /// <summary>Every runner request (seeded and shared, all accounts), shared copies replacing seeds.</summary>
    public IReadOnlyList<SharedRunnerRequest> AllRunnerRequests() => Current.RunnerRequests;

    public SharedRunnerRequest? FindRunnerRequest(string id) =>
        State.RunnerRequests.TryGetValue(id, out var shared) ? shared
        : store.RunnerRequests().FirstOrDefault(r => r.Id == id) is { } seed ? SeededRunnerRequest(seed) : null;

    private SharedRunnerRequest SeededRunnerRequest(RunnerRequestItem r) => new(r, 0, Find(r.ApplicationReference)?.OwnerId);

    /// <summary>True for a request a family made in the portal (not seeded data).</summary>
    public bool IsFamilyRunnerRequest(string id) =>
        State.RunnerRequests.ContainsKey(id) && store.RunnerRequests().All(r => r.Id != id);

    /// <summary>Save a family's runner request; refused when the application already has one in progress.</summary>
    public async Task<bool> RequestRunnerAsync(Actor actor, RunnerRequestItem request, PortalNotification notification)
    {
        if (actor.Problem(Data.Roles.Patient) is not null) return false;
        bool saved;
        await _gate.WaitAsync();
        try
        {
            if (AllRunnerRequests().Any(r => Same(r.Request.ApplicationReference, request.ApplicationReference) && RunnerRules.IsOpen(r.Request.Status)))
                return false;
            // No runner help for a patient whose authorization wasn't approved (M3.2).
            if (Find(request.ApplicationReference) is not { } application || application.OwnerId != actor.UserId) return false;
            // No errand for an application that has ended (M3.7).
            if (application.Application.Status is ApplicationStatus.Completed or ApplicationStatus.Cancelled) return false;
            if (Authorization(application.Application.PatientId) == AuthorizationStanding.Rejected) return false;
            saved = await CommitAsync(State, new Writes(Notifications: [notification with { OwnerId = actor.UserId }],
                RunnerRequests: [new SharedRunnerRequest(request, 1, actor.UserId)], Audit: new(actor, "Requested runner help", request.Id)));
        }
        finally
        {
            _gate.Release();
        }
        if (saved) Changed?.Invoke();
        return saved;
    }

    /// <summary>The family cancels its own request while it still waits for a runner.</summary>
    public async Task<bool> CancelRunnerRequestAsync(Actor actor, string id)
    {
        if (actor.Problem(Data.Roles.Patient) is not null) return false;
        bool saved;
        await _gate.WaitAsync();
        try
        {
            var current = FindRunnerRequest(id);
            if (current is null || !IsFamilyRunnerRequest(id) || current.OwnerId != actor.UserId
                || current.Request.Status != RunnerTaskStatus.PendingAssignment)
                return false;
            var cancelled = current with
            {
                Request = current.Request with { Status = RunnerTaskStatus.Cancelled, ClosedAt = clock.GetLocalNow() },
                Version = current.Version + 1,
            };
            saved = await CommitAsync(State, new Writes(RunnerRequests: [cancelled], Audit: new(actor, "Cancelled runner request", id)));
        }
        finally
        {
            _gate.Release();
        }
        if (saved) Changed?.Invoke();
        return saved;
    }

    /// <summary>Assign (or change) the runner. The application's status never changes with it.</summary>
    public async Task<ReviewResult> AssignRunnerAsync(Actor actor, string id, int expectedVersion, string? runner)
    {
        if (actor.Problem(Data.Roles.Administrator) is string notAllowed) return ReviewResult.Refused(notAllowed);
        ReviewResult result;
        await _gate.WaitAsync();
        try
        {
            var current = FindRunnerRequest(id);
            if (current is null) return ReviewResult.Refused("That runner request doesn't exist.");
            if (current.Version != expectedVersion) return ReviewResult.Stale;
            if (StaffDecisionRules.AssignProblem(current.Request.Status, current.Request.RunnerName, runner) is string problem)
                return ReviewResult.Refused(problem);
            if (Find(current.Request.ApplicationReference) is { } application
                && Authorization(application.Application.PatientId) == AuthorizationStanding.Rejected)
                return ReviewResult.Refused("The patient's authorization wasn't approved, so no runner can be assigned until a new request is approved.");

            var now = clock.GetLocalNow();
            var reassigned = current.Request.Status == RunnerTaskStatus.Assigned;
            var assigned = current with
            {
                Request = current.Request with
                {
                    Status = RunnerTaskStatus.Assigned, RunnerName = runner, AssignedAt = now,
                    Steps = [.. current.Request.Steps ?? [], new RunnerStep(now, RunnerStepKind.Assigned, runner!, null, actor.UserId, actor.Name)],
                },
                Version = current.Version + 1,
            };
            var notice = new PortalNotification($"n-{id}-assigned-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
                reassigned ? "Runner changed" : "Runner assigned",
                $"{runner} will help with “{current.Request.Errand}” for {current.Request.ApplicationReference}.",
                now, false, "/patient/runner");
            var target = Find(current.Request.ApplicationReference);
            var notices = target?.OwnerId is { } owner ? [notice with { PatientId = target.Application.PatientId, OwnerId = owner }] : Array.Empty<PortalNotification>();
            result = await CommitAsync(State, new Writes(Notifications: notices, RunnerRequests: [assigned],
                    Audit: new(actor, reassigned ? $"Changed runner to {runner}" : $"Assigned {runner}", id)))
                ? ReviewResult.Done
                : ReviewResult.SaveFailed;
        }
        finally
        {
            _gate.Release();
        }
        if (result.Succeeded) Changed?.Invoke();
        return result;
    }

    // ---- Runner tasks (D10) ----

    /// <summary>
    /// A runner's tasks: every request assigned to them now, and those they had before (declined,
    /// handed back) for their history. Each shows only what the errand needs.
    /// </summary>
    public IReadOnlyList<RunnerTaskView> RunnerTasksFor(string runner) =>
    [
        .. AllRunnerRequests()
            .Where(r => r.Request.RunnerName == runner || (r.Request.Steps?.Any(s => s.Runner == runner) ?? false))
            .Select(RunnerView),
    ];

    public RunnerTaskView? RunnerTask(string id) => FindRunnerRequest(id) is { } r ? RunnerView(r) : null;

    private RunnerTaskView RunnerView(SharedRunnerRequest r)
    {
        var place = r.Request.Place;
        var hospital = store.Hospitals.FirstOrDefault(h => place.StartsWith(h.Name, StringComparison.OrdinalIgnoreCase));
        var patient = Find(r.Request.ApplicationReference)?.Application.PatientName ?? "";
        return new RunnerTaskView(r, hospital?.District ?? "", patient.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "the family");
    }

    /// <summary>
    /// The assigned runner takes a step: accept, decline (with a reason), on the way, done (with an
    /// optional note) or hand back (with a reason). Declining or handing back returns the request to
    /// the administrator. The application's status never changes.
    /// </summary>
    public async Task<ReviewResult> RunnerStepAsync(Actor actor, string id, int expectedVersion, RunnerStepKind kind, string? note)
    {
        // The runner is the signed-in Runner account (M3.4b): its name must be the assigned runner's.
        if (actor.Problem(Data.Roles.Runner) is string notAllowed) return ReviewResult.Refused(notAllowed);
        var runner = actor.Name;
        ReviewResult result;
        await _gate.WaitAsync();
        try
        {
            var current = FindRunnerRequest(id);
            if (current is null) return ReviewResult.Refused("That errand doesn't exist.");
            if (current.Version != expectedVersion) return ReviewResult.Stale;
            if (RunnerRules.StepProblem(current.Request, runner, kind, note) is string problem) return ReviewResult.Refused(problem);

            var now = clock.GetLocalNow();
            var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            var status = RunnerRules.StatusAfter(kind);
            var updated = current with
            {
                Request = current.Request with
                {
                    Status = status,
                    ClosedAt = status == RunnerTaskStatus.Completed ? now : current.Request.ClosedAt,
                    Steps = [.. current.Request.Steps ?? [], new RunnerStep(now, kind, runner, text, actor.UserId, actor.Name)],
                },
                Version = current.Version + 1,
            };

            var errand = current.Request.Errand;
            var reference = current.Request.ApplicationReference;
            var first = runner.Split(' ')[0];
            PortalNotification? notice = kind switch
            {
                RunnerStepKind.Accepted => new($"n-{id}-accepted-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}", "Runner accepted",
                    $"{first} accepted “{errand}” for {reference}.", now, false, "/patient/runner"),
                RunnerStepKind.Completed => new($"n-{id}-done-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}", "Runner errand done",
                    $"{first} finished “{errand}” for {reference}.", now, false, "/patient/runner"),
                RunnerStepKind.Declined or RunnerStepKind.HandedBack => new($"n-{id}-handback-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}", "Finding another runner",
                    $"Your runner can't do “{errand}”. The office will assign another.", now, false, "/patient/runner"),
                _ => null
            };
            var application = Find(reference);
            var notices = notice is not null && application?.OwnerId is { } owner
                ? [notice with { PatientId = application.Application.PatientId, OwnerId = owner }] : Array.Empty<PortalNotification>();
            result = await CommitAsync(State, new Writes(Notifications: notices, RunnerRequests: [updated], Audit: new(actor, $"Runner step: {kind}", id)))
                ? ReviewResult.Done
                : ReviewResult.SaveFailed;
        }
        finally
        {
            _gate.Release();
        }
        if (result.Succeeded) Changed?.Invoke();
        return result;
    }

    // ---- Storage ----

    private sealed record Writes(
        IReadOnlyList<SharedApplication>? Applications = null,
        IReadOnlyList<DocumentUpload>? Uploads = null,
        IReadOnlyList<PortalNotification>? Notifications = null,
        IReadOnlyList<SharedAuthorization>? Authorizations = null,
        IReadOnlyList<SharedRunnerRequest>? RunnerRequests = null,
        AuditNote? Audit = null);

    /// <summary>Who made a change and what it was, for the audit log (M3.7).</summary>
    private sealed record AuditNote(Actor Actor, string Action, string Target);

    /// <summary>Write the changes in one transaction; on success swap in the new read model. Call under the gate.</summary>
    private async Task<bool> CommitAsync(WorkflowState state, Writes writes)
    {
        var applications = writes.Applications ?? [];
        var uploads = writes.Uploads ?? [];
        var notifications = writes.Notifications ?? [];
        var authorizations = writes.Authorizations ?? [];
        var runnerRequests = writes.RunnerRequests ?? [];
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            foreach (var a in applications)
            {
                var row = await db.Applications.FindAsync(a.Reference);
                if (row is null) db.Applications.Add(row = new StoredApplication { Reference = a.Reference });
                // Only update the row from the version this change started from (M3.6).
                else db.Entry(row).Property(r => r.Version).OriginalValue = a.Version - 1;
                row.PatientId = a.Application.PatientId;
                row.Status = (int)a.Application.Status;
                row.Version = a.Version;
                row.Json = JsonSerializer.Serialize(a, Json);
            }
            foreach (var u in uploads)
            {
                var row = await db.Uploads.FindAsync(u.Id);
                if (row is null) db.Uploads.Add(row = new StoredUpload { Id = u.Id });
                row.Reference = u.ApplicationReference;
                row.Json = JsonSerializer.Serialize(u, Json);
            }
            foreach (var n in notifications)
                db.Notifications.Add(new StoredNotification { Id = n.Id, Json = JsonSerializer.Serialize(n, Json) });
            foreach (var a in authorizations) await UpsertAsync(db, db.Authorizations, a.Id, a.Version, JsonSerializer.Serialize(a, Json));
            foreach (var r in runnerRequests) await UpsertAsync(db, db.RunnerRequests, r.Id, r.Version, JsonSerializer.Serialize(r, Json));
            if (writes.Audit is { } audit)
                db.AuditEntries.Add(new StoredAuditEntry
                {
                    AtUtc = clock.GetUtcNow().UtcDateTime, ActorId = audit.Actor.UserId, ActorName = audit.Actor.Name,
                    Role = audit.Actor.Role, Action = audit.Action, Target = audit.Target,
                });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // The database moved on (another server process, M3.6): nothing is written; reload what it holds.
            logger.LogWarning(ex, "A shared record changed in the database since it was read; reloading.");
            await using var fresh = await factory.CreateDbContextAsync();
            await LoadStateAsync(fresh);
            return false;
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.Sqlite.SqliteException or IOException or InvalidOperationException)
        {
            logger.LogError(ex, "The shared workflow store could not be updated.");
            return false;
        }

        var nextApplications = new Dictionary<string, SharedApplication>(state.Applications, StringComparer.OrdinalIgnoreCase);
        foreach (var a in applications) nextApplications[a.Reference] = a;
        var nextAuthorizations = new Dictionary<string, SharedAuthorization>(state.Authorizations);
        foreach (var a in authorizations) nextAuthorizations[a.Id] = a;
        var nextRunnerRequests = new Dictionary<string, SharedRunnerRequest>(state.RunnerRequests);
        foreach (var r in runnerRequests) nextRunnerRequests[r.Id] = r;
        var changedIds = uploads.Select(u => u.Id).ToHashSet();
        Volatile.Write(ref _state, new WorkflowState(
            nextApplications,
            [.. state.Uploads.Where(u => !changedIds.Contains(u.Id)), .. uploads],
            [.. notifications.OrderByDescending(n => n.CreatedAt), .. state.Notifications],
            nextAuthorizations,
            nextRunnerRequests));
        return true;
    }

    private static async Task UpsertAsync(WorkflowDbContext db, DbSet<StoredRecord> set, string id, int version, string json)
    {
        var row = await set.FindAsync(id);
        if (row is null)
        {
            set.Add(new StoredRecord { Id = id, Version = version, Json = json });
            return;
        }
        // Only update the row from the version this change started from (M3.6).
        db.Entry(row).Property(r => r.Version).OriginalValue = version - 1;
        row.Version = version;
        row.Json = json;
    }

    // ---- Helpers ----

    /// <summary>Record which account made a history entry (M3.4b).</summary>
    private static ApplicationEvent Stamp(ApplicationEvent e, Actor actor) => e with { ActorId = actor.UserId, ActorName = actor.Name };

    /// <summary>A seeded application without a written checklist: its program's documents, the first ones ready.</summary>
    private ApplicationChecklist GeneratedChecklist(AssistanceApplication application)
    {
        var program = store.Programs.FirstOrDefault(p => p.Id == application.ProgramId);
        var documents = program?.RequiredDocuments ?? [];
        return new ApplicationChecklist(application.ReferenceNumber, application.ProgramName, application.PatientName,
            documents.Select((d, i) => new ChecklistItem(d, i < application.DocumentsReady ? DocumentStatus.Complete : DocumentStatus.Missing,
                true, "See the program's requirements")).ToList());
    }

    private static DocumentUpload? Latest(IEnumerable<DocumentUpload> uploads, string reference, string documentName) =>
        uploads.Where(u => Same(u.ApplicationReference, reference) && u.DocumentName == documentName).MaxBy(u => u.Version);

    /// <summary>A notification about one patient's application, unique per change (repeated rounds don't collide).</summary>
    private static PortalNotification Notice(SharedApplication application, string kind, string title, string message, DateTimeOffset now) =>
        new($"n-{application.Reference}-{kind}-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}", title, message, now, false,
            $"/patient/applications/{application.Reference}", application.Application.PatientId, application.OwnerId);

    public static string AddMissing(int count) =>
        count == 1 ? "Add the missing document" : $"Add the {count} missing documents";

    private static string Lower(string name) => name.Length > 0 ? char.ToLowerInvariant(name[0]) + name[1..] : name;

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Retention for the shared store (M3.7), from <c>Retention:NotificationDays</c> and <c>Retention:AuditDays</c>.</summary>
public sealed record WorkflowOptions(int NotificationDays = 180, int AuditDays = 365);
