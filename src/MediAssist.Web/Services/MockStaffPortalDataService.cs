using MediAssist.Web.Models;
using MediAssist.Web.Navigation;

namespace MediAssist.Web.Services;

/// <summary>
/// Fictional staff data for the UI prototype. Reference numbers match the patient mock data
/// so the three portals tell one story. Times are anchored to the start of the day.
/// </summary>
public sealed class MockStaffPortalDataService(TimeProvider timeProvider) : IStaffPortalDataService
{
    private DateTimeOffset Today
    {
        get
        {
            var now = timeProvider.GetLocalNow();
            return new DateTimeOffset(now.Date, now.Offset);
        }
    }

    public Task<StaffUser> GetUserAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner
            ? new StaffUser("Paolo", "Dizon")
            : new StaffUser("Ana", "Reyes"));

    public Task<int> GetUnreadCountAsync(PortalRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(role == PortalRole.Runner ? 1 : 4);

    public Task<IReadOnlyList<ReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        return Task.FromResult<IReadOnlyList<ReviewQueueItem>>(
        [
            new("MA-2026-00395", "Lia Santos", "Diagnostic Procedure Aid", "Northridge Children's Hospital",
                ApplicationStatus.Submitted, 2, 3, today.AddDays(-4).AddHours(9), AuthorizationPending: true),
            new("MA-2026-00431", "Maria Santos", "Medicine Cost Assistance", "Bayview Medical Center",
                ApplicationStatus.UnderReview, 4, 4, today.AddDays(-1).AddHours(10), AuthorizationPending: false),
            new("MA-2026-00452", "Ramon Villanueva", "Hospital Bill Support", "San Isidro General Hospital",
                ApplicationStatus.Submitted, 3, 4, today.AddDays(-2).AddHours(15), AuthorizationPending: false),
            new("MA-2026-00418", "Jose Santos", "Hospital Bill Support", "San Isidro General Hospital",
                ApplicationStatus.NeedsCorrection, 3, 5, today.AddHours(-3), AuthorizationPending: false),
        ]);
    }

    public Task<IReadOnlyList<RunnerRequestItem>> GetRunnerRequestsAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        return Task.FromResult<IReadOnlyList<RunnerRequestItem>>(
        [
            new("rr-31", "MA-2026-00452", "Deliver original hospital bill to the social service office",
                "San Isidro General Hospital", today.AddDays(1).AddHours(14), RunnerTaskStatus.PendingAssignment, null),
            new("rr-29", "MA-2026-00431", "Pick up approved medicine voucher",
                "Bayview Medical Center", today.AddHours(16), RunnerTaskStatus.Accepted, "Paolo Dizon"),
            new("rr-27", "MA-2026-00395", "Submit laboratory quotation",
                "Northridge Children's Hospital", today.AddDays(2).AddHours(10), RunnerTaskStatus.Declined, "Liza Manalo"),
        ]);
    }

    public Task<IReadOnlyList<RunnerTask>> GetRunnerTasksAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        return Task.FromResult<IReadOnlyList<RunnerTask>>(
        [
            new("rt-29", "MA-2026-00431", "Pick up approved medicine voucher",
                "Bayview Medical Center, social service office", "Bayview district", today.AddHours(16),
                RunnerTaskStatus.Accepted, "Maria"),
            new("rt-33", "MA-2026-00317", "Return signed acknowledgement form",
                "San Isidro General Hospital, billing office", "San Isidro", today.AddDays(1).AddHours(11),
                RunnerTaskStatus.Assigned, "Jose"),
            new("rt-24", "MA-2026-00362", "Deliver laboratory results to the program office",
                "Bayview Medical Center, laboratory", "Bayview district", today.AddDays(-1).AddHours(15),
                RunnerTaskStatus.Completed, "Maria"),
        ]);
    }
}
