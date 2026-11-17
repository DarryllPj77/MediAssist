using MediAssist.Web.Models;
using MediAssist.Web.Navigation;

namespace MediAssist.Web.Services;

/// <summary>
/// The staff portals' view. Reviews, authorization requests, runner requests and runner tasks come
/// from the shared <see cref="WorkflowService"/> (D9a, D9b, D10), so staff see what families send
/// from any browser. The runner portal shows the runner chosen in settings (<see cref="RunnerSession"/>).
/// </summary>
public sealed class MockStaffPortalDataService(WorkflowService workflow, RunnerSession runner) : IStaffPortalDataService
{
    public Task<StaffUser> GetUserAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner
            ? new StaffUser(runner.Current.Name.Split(' ')[0], runner.Current.Name[(runner.Current.Name.IndexOf(' ') + 1)..])
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

    public Task<IReadOnlyList<RunnerTaskView>> GetRunnerTasksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(workflow.RunnerTasksFor(runner.Current.Name));
}
