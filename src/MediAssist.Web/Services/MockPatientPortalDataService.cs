using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// Fictional data for the UI prototype. Every person, hospital, program and reference number
/// here is invented. Timestamps are anchored to the start of the current day so the
/// prerendered and interactive renders show identical relative dates.
/// </summary>
public sealed class MockPatientPortalDataService(TimeProvider timeProvider) : IPatientPortalDataService
{
    private static readonly Guid UserId = Guid.Parse("6f1d2c1e-0000-4000-8000-000000000001");
    private static readonly Guid MariaId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a1");
    private static readonly Guid JoseId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a2");
    private static readonly Guid LiaId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a3");

    private DateTimeOffset Today
    {
        get
        {
            var now = timeProvider.GetLocalNow();
            return new DateTimeOffset(now.Date, now.Offset);
        }
    }

    public Task<PortalUser> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new PortalUser(UserId, "Maria", "Santos"));

    public Task<IReadOnlyList<PatientProfile>> GetPatientProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PatientProfile>>(
        [
            new(MariaId, "Maria Santos", "You", AuthorizationStatus.Self),
            new(JoseId, "Jose Santos", "Father", AuthorizationStatus.Authorized),
            new(LiaId, "Lia Santos", "Daughter", AuthorizationStatus.PendingReview),
        ]);

    public Task<IReadOnlyList<AssistanceApplication>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        IReadOnlyList<AssistanceApplication> applications =
        [
            new("MA-2026-00418", JoseId, "Jose Santos", "San Isidro General Hospital", "Hospital Bill Support",
                ApplicationStatus.NeedsCorrection, 3, 5, today.AddHours(-3).AddMinutes(-20),
                "Re-upload a clearer copy of the medical abstract"),
            new("MA-2026-00431", MariaId, "Maria Santos", "Bayview Medical Center", "Medicine Cost Assistance",
                ApplicationStatus.UnderReview, 4, 4, today.AddDays(-1).AddHours(10),
                "Wait for the document review to finish"),
            new("MA-2026-00437", JoseId, "Jose Santos", "San Isidro General Hospital", "Dialysis Session Support",
                ApplicationStatus.Draft, 1, 4, today.AddDays(-2).AddHours(16),
                "Finish step 4: Prepare documents"),
            new("MA-2026-00395", LiaId, "Lia Santos", "Northridge Children's Hospital", "Diagnostic Procedure Aid",
                ApplicationStatus.Submitted, 2, 3, today.AddDays(-4).AddHours(9),
                "Patient authorization is being reviewed"),
            new("MA-2026-00362", MariaId, "Maria Santos", "Bayview Medical Center", "Laboratory Fee Assistance",
                ApplicationStatus.ReadyForProcessing, 3, 3, today.AddDays(-9).AddHours(14),
                "No action needed from you"),
            new("MA-2026-00317", JoseId, "Jose Santos", "San Isidro General Hospital", "Medicine Cost Assistance",
                ApplicationStatus.Completed, 4, 4, today.AddDays(-21).AddHours(11),
                "Internal processing finished"),
        ];
        return Task.FromResult(applications);
    }

    public Task<IReadOnlyList<ActionItem>> GetActionItemsAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        IReadOnlyList<ActionItem> items =
        [
            new("act-1", ActionPriority.High,
                "Medical abstract needs a clearer copy",
                "The reviewer could not read the attending physician's signature on page 2.",
                "MA-2026-00418", "Jose Santos",
                "Upload corrected document", "/patient/applications/MA-2026-00418",
                today.AddDays(3)),
            new("act-2", ActionPriority.Medium,
                "Draft application is waiting for documents",
                "3 of 4 required documents are still missing. You can submit now and add them later.",
                "MA-2026-00437", "Jose Santos",
                "Continue application", "/patient/applications/new?draft=MA-2026-00437"),
        ];
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<PortalNotification>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        IReadOnlyList<PortalNotification> notifications =
        [
            new("n-1", "Correction requested", "MA-2026-00418 needs a clearer medical abstract.", today.AddHours(-3), false),
            new("n-2", "Review started", "MA-2026-00431 is now under review.", today.AddDays(-1).AddHours(10), false),
            new("n-3", "Authorization received", "Lia Santos's authorization request is being reviewed.", today.AddDays(-4), false),
            new("n-4", "Application completed", "Internal processing for MA-2026-00317 is finished.", today.AddDays(-21), true),
        ];
        return Task.FromResult(notifications);
    }
}
