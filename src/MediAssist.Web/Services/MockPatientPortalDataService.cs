using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// The patient portal's view of the shared <see cref="MockPortalStore"/>: only the account
/// holder's household, as a real implementation would scope it to the signed-in user.
/// </summary>
public sealed class MockPatientPortalDataService(MockPortalStore store) : IPatientPortalDataService
{
    public Task<PortalUser> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.CurrentUser);

    public Task<IReadOnlyList<PatientProfile>> GetPatientProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Household);

    public Task<IReadOnlyList<AssistanceApplication>> GetApplicationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(HouseholdApplications());

    public Task<IReadOnlyList<ActionItem>> GetActionItemsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.ActionItems());

    public Task<IReadOnlyList<PortalNotification>> GetNotificationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Notifications());

    public Task<IReadOnlyList<AssistanceProgram>> GetProgramsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Programs);

    /// <summary>Checklists that still need something: the household's applications with documents left to prepare.</summary>
    public Task<IReadOnlyList<ApplicationChecklist>> GetDocumentChecklistsAsync(CancellationToken cancellationToken = default)
    {
        var references = HouseholdApplications().Select(a => a.ReferenceNumber).ToHashSet();
        IReadOnlyList<ApplicationChecklist> open = store.Checklists
            .Where(c => references.Contains(c.ApplicationReference) && c.ReadyCount < c.Items.Count)
            .ToList();
        return Task.FromResult(open);
    }

    public Task<ApplicationDetail?> GetApplicationAsync(string reference, CancellationToken cancellationToken = default)
    {
        var application = HouseholdApplications()
            .FirstOrDefault(a => string.Equals(a.ReferenceNumber, reference, StringComparison.OrdinalIgnoreCase));
        if (application is null)
            return Task.FromResult<ApplicationDetail?>(null);

        var detail = new ApplicationDetail(
            application,
            store.Household.FirstOrDefault(p => p.Id == application.PatientId),
            store.Programs.FirstOrDefault(p => p.Id == application.ProgramId),
            store.Checklists.FirstOrDefault(c => c.ApplicationReference == application.ReferenceNumber),
            History(application.ReferenceNumber),
            store.RunnerRequests().Where(r => r.ApplicationReference == application.ReferenceNumber).ToList());
        return Task.FromResult<ApplicationDetail?>(detail);
    }

    public Task<IReadOnlyList<ApplicationEvent>> GetApplicationHistoryAsync(string reference, CancellationToken cancellationToken = default) =>
        Task.FromResult(HouseholdApplications().Any(a => a.ReferenceNumber == reference)
            ? History(reference)
            : (IReadOnlyList<ApplicationEvent>)[]);

    private IReadOnlyList<AssistanceApplication> HouseholdApplications() =>
        store.Applications().Where(a => store.IsHousehold(a.PatientId)).ToList();

    private IReadOnlyList<ApplicationEvent> History(string reference) =>
        store.Events().Where(e => e.ApplicationReference == reference).OrderBy(e => e.At).ToList();
}
