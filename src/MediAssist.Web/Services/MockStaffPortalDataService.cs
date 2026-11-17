using MediAssist.Web.Models;
using MediAssist.Web.Navigation;

namespace MediAssist.Web.Services;

/// <summary>
/// The staff portals' view. Reviews, authorization requests and runner requests come from the
/// shared <see cref="WorkflowService"/> (D9a, D9b), so administrators see what families send from
/// any browser; staff names and runner tasks still come from the seeded <see cref="MockPortalStore"/> (D10).
/// </summary>
public sealed class MockStaffPortalDataService(MockPortalStore store, WorkflowService workflow) : IStaffPortalDataService
{
    public Task<StaffUser> GetUserAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner
            ? new StaffUser("Paolo", "Dizon")
            : new StaffUser("Ana", "Reyes"));

    public Task<int> GetUnreadCountAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner ? 1 : 4);

    public Task<IReadOnlyList<ReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReviewQueueItem> queue = workflow.All()
            .Where(a => ReviewRules.Open.Contains(a.Application.Status))
            .Select(a =>
            {
                var checklist = workflow.Effective(a);
                return new ReviewQueueItem(
                    a.Reference, a.Application.PatientName, a.Application.ProgramName, a.Application.HospitalName, a.Application.Status,
                    checklist.ReadyCount, checklist.Items.Count, a.Application.LastUpdated,
                    AuthorizationPending: workflow.AuthorizationPending(a.Application.PatientId),
                    AwaitingDecision: checklist.Items.Count(i => i.Status == DocumentStatus.PendingReview));
            })
            .ToList();
        return Task.FromResult(queue);
    }

    public Task<ReviewDetail?> GetReviewAsync(string reference, CancellationToken cancellationToken = default)
    {
        var current = workflow.Find(reference);
        if (current is null || current.Application.Status == ApplicationStatus.Draft) return Task.FromResult<ReviewDetail?>(null);

        var latest = current.Checklist.Items
            .Select(i => workflow.LatestUpload(current.Reference, i.Name))
            .OfType<DocumentUpload>()
            .ToDictionary(u => u.DocumentName);
        return Task.FromResult<ReviewDetail?>(new ReviewDetail(
            current, workflow.Effective(current), latest, workflow.History(current),
            workflow.AuthorizationPending(current.Application.PatientId)));
    }

    /// <summary>Authorization requests from every family (D9b), newest first.</summary>
    public Task<IReadOnlyList<SharedAuthorization>> GetAuthorizationRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(workflow.AllAuthorizations());

    /// <summary>Runner requests from every family (D9b), seeded ones included.</summary>
    public Task<IReadOnlyList<SharedRunnerRequest>> GetRunnerRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(workflow.AllRunnerRequests());

    public Task<IReadOnlyList<RunnerTask>> GetRunnerTasksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.RunnerTasks());
}
