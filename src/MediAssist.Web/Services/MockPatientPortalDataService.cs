using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// The patient portal's view: the shared, read-only <see cref="MockPortalStore"/> (scoped to the
/// account holder's household) merged with this user's own <see cref="ApplicationWorkspace"/>
/// (drafts and submissions kept in their browser). A local entry replaces a seed entry with the
/// same reference, so a resumed or submitted seed draft shows its latest state everywhere.
/// </summary>
public sealed class MockPatientPortalDataService(MockPortalStore store, ApplicationWorkspace workspace) : IPatientPortalDataService
{
    public Task<PortalUser> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.CurrentUser);

    public Task<IReadOnlyList<PatientProfile>> GetPatientProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Household);

    public Task<IReadOnlyList<AssistanceApplication>> GetApplicationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Applications());

    /// <summary>The seeded to-dos that still apply (a submitted draft no longer needs finishing).</summary>
    public Task<IReadOnlyList<ActionItem>> GetActionItemsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = Applications().ToDictionary(a => a.ReferenceNumber, a => a.Status);
        IReadOnlyList<ActionItem> items = store.ActionItems()
            .Where(i => statuses.TryGetValue(i.ApplicationReference, out var s)
                        && s is ApplicationStatus.Draft or ApplicationStatus.NeedsCorrection)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<PortalNotification>> GetNotificationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PortalNotification>>([.. workspace.Notifications, .. store.Notifications()]);

    public Task<IReadOnlyList<AssistanceProgram>> GetProgramsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Programs);

    public Task<IReadOnlyList<string>> GetHospitalsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(store.Applications().Select(a => a.HospitalName).Distinct().Order().ToList());

    /// <summary>Checklists that still need something: the household's applications with documents left to prepare.</summary>
    public Task<IReadOnlyList<ApplicationChecklist>> GetDocumentChecklistsAsync(CancellationToken cancellationToken = default)
    {
        var references = Applications().Select(a => a.ReferenceNumber).ToHashSet();
        IReadOnlyList<ApplicationChecklist> open = Checklists()
            .Where(c => references.Contains(c.ApplicationReference) && c.ReadyCount < c.Items.Count)
            .ToList();
        return Task.FromResult(open);
    }

    public Task<ApplicationDetail?> GetApplicationAsync(string reference, CancellationToken cancellationToken = default)
    {
        var application = Applications()
            .FirstOrDefault(a => string.Equals(a.ReferenceNumber, reference, StringComparison.OrdinalIgnoreCase));
        if (application is null)
            return Task.FromResult<ApplicationDetail?>(null);

        var detail = new ApplicationDetail(
            application,
            store.Household.FirstOrDefault(p => p.Id == application.PatientId),
            store.Programs.FirstOrDefault(p => p.Id == application.ProgramId),
            Checklists().FirstOrDefault(c => c.ApplicationReference == application.ReferenceNumber),
            History(application.ReferenceNumber),
            store.RunnerRequests().Where(r => r.ApplicationReference == application.ReferenceNumber).ToList());
        return Task.FromResult<ApplicationDetail?>(detail);
    }

    public Task<IReadOnlyList<ApplicationEvent>> GetApplicationHistoryAsync(string reference, CancellationToken cancellationToken = default) =>
        Task.FromResult(Applications().Any(a => a.ReferenceNumber == reference)
            ? History(reference)
            : (IReadOnlyList<ApplicationEvent>)[]);

    // ---- The merged view ----

    /// <summary>Local entries keyed by reference: submitted applications, then drafts (as applications).</summary>
    private Dictionary<string, AssistanceApplication> LocalApplications()
    {
        var local = new Dictionary<string, AssistanceApplication>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in workspace.Submitted) local[s.Application.ReferenceNumber] = s.Application;
        foreach (var d in workspace.Drafts)
        {
            var asApplication = workspace.DraftAsApplication(d);
            local.TryAdd(asApplication.ReferenceNumber, asApplication);
        }
        return local;
    }

    private IReadOnlyList<AssistanceApplication> Applications()
    {
        var local = LocalApplications();
        var seeded = store.Applications()
            .Where(a => store.IsHousehold(a.PatientId) && !local.ContainsKey(a.ReferenceNumber));
        return [.. local.Values, .. seeded];
    }

    private IReadOnlyList<ApplicationChecklist> Checklists()
    {
        var local = new Dictionary<string, ApplicationChecklist>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in workspace.Submitted)
            if (s.Checklist is not null) local[s.Application.ReferenceNumber] = s.Checklist;
        foreach (var d in workspace.Drafts)
            if (workspace.DraftChecklist(d) is { } c) local.TryAdd(c.ApplicationReference, c);

        return [.. local.Values, .. store.Checklists.Where(c => !local.ContainsKey(c.ApplicationReference))];
    }

    private IReadOnlyList<ApplicationEvent> History(string reference)
    {
        if (workspace.Submitted.FirstOrDefault(s => string.Equals(s.Application.ReferenceNumber, reference, StringComparison.OrdinalIgnoreCase)) is { } submitted)
            return submitted.Events.OrderBy(e => e.At).ToList();
        if (workspace.Drafts.FirstOrDefault(d => string.Equals(d.SourceReference ?? d.Id, reference, StringComparison.OrdinalIgnoreCase)) is { } draft)
            return workspace.EventsFor(draft);
        return store.Events().Where(e => e.ApplicationReference == reference).OrderBy(e => e.At).ToList();
    }
}
