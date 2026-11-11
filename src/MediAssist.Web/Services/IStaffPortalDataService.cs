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
    Task<IReadOnlyList<ReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RunnerRequestItem>> GetRunnerRequestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RunnerTask>> GetRunnerTasksAsync(CancellationToken cancellationToken = default);
}
