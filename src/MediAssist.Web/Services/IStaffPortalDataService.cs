using MediAssist.Web.Models;
using MediAssist.Web.Navigation;

namespace MediAssist.Web.Services;

/// <summary>
/// Read model for the Administrator and Medical Runner portals. The prototype uses
/// <see cref="MockStaffPortalDataService"/> with fictional data; no authentication yet.
/// </summary>
public interface IStaffPortalDataService
{
    Task<StaffUser> GetUserAsync(PortalRole role, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(PortalRole role, CancellationToken cancellationToken = default);
    /// <summary>Applications an administrator works on (submitted through ready for processing), from the shared store.</summary>
    Task<IReadOnlyList<ReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default);
    /// <summary>One application for review (D9a), or null when there is none with that reference.</summary>
    Task<ReviewDetail?> GetReviewAsync(string reference, CancellationToken cancellationToken = default);
    /// <summary>Authorization requests waiting for or given an administrator decision (D9b), with what the reviewer needs.</summary>
    Task<IReadOnlyList<SharedAuthorization>> GetAuthorizationRequestsAsync(CancellationToken cancellationToken = default);
    /// <summary>Runner requests from every family (D9b), with versions for assignment.</summary>
    Task<IReadOnlyList<SharedRunnerRequest>> GetRunnerRequestsAsync(CancellationToken cancellationToken = default);
    /// <summary>The chosen runner's tasks (D10): assigned now, or had before, with only what each errand needs.</summary>
    Task<IReadOnlyList<RunnerTaskView>> GetRunnerTasksAsync(CancellationToken cancellationToken = default);
}
