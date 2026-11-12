using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// One user's drafts and submitted applications. Registered per circuit (scoped), loaded from
/// that user's browser storage once the page is interactive and written back on every change,
/// so nothing mutable is shared between users. <see cref="Changed"/> tells pages to re-query.
/// <para>
/// The seeded <see cref="MockPortalStore"/> stays read-only; a local entry with the same reference
/// (a resumed or submitted seed draft) takes its place in the merged view.
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

    public IReadOnlyList<WizardDraft> Drafts => _snapshot.Drafts;
    public IReadOnlyList<LocalApplication> Submitted => _snapshot.Submitted;
    public IReadOnlyList<PortalNotification> Notifications => _snapshot.Notifications;

    /// <summary>Read the user's saved workspace once; later calls return the same task.</summary>
    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        _snapshot = await storage.LoadAsync() ?? WorkspaceSnapshot.Empty;
        IsLoaded = true;
        Changed?.Invoke();
    }

    public WizardDraft? FindDraft(string id) =>
        _snapshot.Drafts.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

    public string NewDraftId() =>
        ReferenceGenerator.NewDraftId(_snapshot.Drafts.Select(d => d.Id));

    /// <summary>Create or update a draft (stamped with the current time) and persist it.</summary>
    public async Task<WizardDraft> SaveDraftAsync(WizardDraft draft)
    {
        var saved = draft with { UpdatedAt = clock.GetLocalNow() };
        _snapshot = _snapshot with { Drafts = [saved, .. _snapshot.Drafts.Where(d => d.Id != saved.Id)] };
        await PersistAsync();
        return saved;
    }

    /// <summary>
    /// Mock submission: the draft becomes a Submitted application with a reference number, a
    /// checklist (missing documents allowed) and its history; a notification confirms it.
    /// Returns the reference. The caller validates the required choices first.
    /// </summary>
    public async Task<string> SubmitAsync(WizardDraft draft)
    {
        var now = clock.GetLocalNow();
        var patient = store.Household.First(p => p.Id == draft.PatientId);
        var program = store.Programs.First(p => p.Id == draft.ProgramId);
        var seed = draft.SourceReference is null ? null
            : store.Applications().FirstOrDefault(a => a.ReferenceNumber == draft.SourceReference);

        var reference = draft.SourceReference ?? ReferenceGenerator.NextReference(
            store.Applications().Select(a => a.ReferenceNumber).Concat(_snapshot.Submitted.Select(s => s.Application.ReferenceNumber)),
            now);

        var checklist = ChecklistFor(reference, program, patient.FullName, draft.SourceReference);
        var ready = checklist.ReadyCount;
        var required = checklist.Items.Count;
        var missing = required - ready;

        var application = new AssistanceApplication(
            reference, patient.Id, patient.FullName, draft.Hospital!, program.Name,
            ApplicationStatus.Submitted, ready, required, now,
            missing > 0 ? $"Add the {missing} missing {(missing == 1 ? "document" : "documents")}" : "Wait for the review to start",
            program.Id, seed?.CreatedAt ?? draft.CreatedAt);

        IReadOnlyList<ApplicationEvent> history =
        [
            .. EventsFor(draft, reference),
            new(reference, now, ApplicationEventKind.Submitted, "Application submitted", "You",
                missing > 0 ? $"Sent with {ready} of {required} documents. The rest can follow." : $"Sent with all {required} documents.",
                ApplicationStatus.Submitted),
        ];

        var notification = new PortalNotification(
            $"n-{reference}", "Application submitted", $"{reference} was sent for review.", now, false);

        _snapshot = _snapshot with
        {
            Drafts = _snapshot.Drafts.Where(d => d.Id != draft.Id).ToList(),
            Submitted = [new LocalApplication(application, checklist, history), .. _snapshot.Submitted.Where(s => s.Application.ReferenceNumber != reference)],
            Notifications = [notification, .. _snapshot.Notifications],
        };
        await PersistAsync();
        return reference;
    }

    // ---- Drafts as applications, for the tracking pages ----

    /// <summary>A draft shown as an application (status Draft); unchosen fields say so.</summary>
    public AssistanceApplication DraftAsApplication(WizardDraft draft)
    {
        var patient = store.Household.FirstOrDefault(p => p.Id == draft.PatientId);
        var program = store.Programs.FirstOrDefault(p => p.Id == draft.ProgramId);
        var checklist = program is null ? null : ChecklistFor(draft.Id, program, patient?.FullName ?? "the patient", draft.SourceReference);
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

    public ApplicationChecklist? DraftChecklist(WizardDraft draft)
    {
        var program = store.Programs.FirstOrDefault(p => p.Id == draft.ProgramId);
        if (program is null) return null;
        var patient = store.Household.FirstOrDefault(p => p.Id == draft.PatientId);
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

    /// <summary>The seed checklist for a continued application, else the program's documents, all still to prepare.</summary>
    private ApplicationChecklist ChecklistFor(string reference, AssistanceProgram program, string patientName, string? sourceReference)
    {
        if (sourceReference is not null && store.Checklists.FirstOrDefault(c => c.ApplicationReference == sourceReference) is { } seeded)
            return seeded with { ApplicationReference = reference };

        return new ApplicationChecklist(reference, program.Name, patientName,
            program.RequiredDocuments.Select(d => new ChecklistItem(d, DocumentStatus.Missing, true, "See the program's requirements")).ToList());
    }

    private async Task PersistAsync()
    {
        await storage.SaveAsync(_snapshot);
        Changed?.Invoke();
    }
}
