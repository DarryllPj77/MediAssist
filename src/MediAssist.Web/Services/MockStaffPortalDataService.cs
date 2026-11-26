using MediAssist.Web.Models;
using MediAssist.Web.Navigation;

namespace MediAssist.Web.Services;

/// <summary>
/// The staff portals' view. Reviews, authorization requests, runner requests and runner tasks come
/// from the shared <see cref="WorkflowService"/> (D9a, D9b, D10), so staff see what families send
/// from any browser. Staff names and the runner come from the signed-in account (M3.4).
/// </summary>
public sealed class MockStaffPortalDataService(WorkflowService workflow, RunnerSession runner,
    Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider auth) : IStaffPortalDataService
{
    /// <summary>The signed-in staff member's name (M3.4).</summary>
    public async Task<StaffUser> GetUserAsync(PortalRole role, CancellationToken cancellationToken = default)
    {
        var user = (await auth.GetAuthenticationStateAsync()).User;
        return new StaffUser(user.FindFirst(Data.AppClaims.FirstName)?.Value ?? "", user.FindFirst(Data.AppClaims.LastName)?.Value ?? "");
    }

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
                    AwaitingDecision: checklist.Items.Count(i => i.Status == DocumentStatus.PendingReview),
                    AuthorizationRejected: workflow.Authorization(a.Application.PatientId) == AuthorizationStanding.Rejected);
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
            workflow.Authorization(current.Application.PatientId)));
    }

    /// <summary>Authorization requests from every family (D9b), newest first.</summary>
    public Task<IReadOnlyList<SharedAuthorization>> GetAuthorizationRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(workflow.AllAuthorizations());

    /// <summary>Runner requests from every family (D9b), seeded ones included.</summary>
    public Task<IReadOnlyList<SharedRunnerRequest>> GetRunnerRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(workflow.AllRunnerRequests());

    public async Task<IReadOnlyList<RunnerTaskView>> GetRunnerTasksAsync(CancellationToken cancellationToken = default)
    {
        await runner.EnsureLoadedAsync();
        return runner.Current is { } current ? workflow.RunnerTasksFor(current.Name) : [];
    }
}
