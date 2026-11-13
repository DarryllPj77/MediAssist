using MediAssist.Web.Models;
using MediAssist.Web.Navigation;

namespace MediAssist.Web.Services;

/// <summary>
/// The staff portals' view of the shared <see cref="MockPortalStore"/>. The review queue is
/// derived from the same applications the patient sees, so the three portals tell one story.
/// </summary>
public sealed class MockStaffPortalDataService(MockPortalStore store) : IStaffPortalDataService
{
    private static readonly ApplicationStatus[] Reviewable =
        [ApplicationStatus.Submitted, ApplicationStatus.UnderReview, ApplicationStatus.NeedsCorrection];

    public Task<StaffUser> GetUserAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner
            ? new StaffUser("Paolo", "Dizon")
            : new StaffUser("Ana", "Reyes"));

    public Task<int> GetUnreadCountAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner ? 1 : 4);

    public Task<IReadOnlyList<ReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReviewQueueItem> queue = store.Applications()
            .Where(a => Reviewable.Contains(a.Status))
            .Select(a => new ReviewQueueItem(
                a.ReferenceNumber, a.PatientName, a.ProgramName, a.HospitalName, a.Status,
                a.DocumentsReady, a.DocumentsRequired, a.LastUpdated,
                AuthorizationPending: store.AuthorizationRequests().Any(r => r.PatientId == a.PatientId && r.Status == AuthorizationRequestStatus.Pending)))
            .ToList();
        return Task.FromResult(queue);
    }

    /// <summary>
    /// Authorization requests for administrator review (D9). Seeded requests only: requests made in
    /// a patient's browser (D4) are not visible to staff until shared persistence exists.
    /// </summary>
    public Task<IReadOnlyList<AuthorizationRequest>> GetAuthorizationRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.AuthorizationRequests());

    public Task<IReadOnlyList<RunnerRequestItem>> GetRunnerRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.RunnerRequests());

    public Task<IReadOnlyList<RunnerTask>> GetRunnerTasksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.RunnerTasks());
}
