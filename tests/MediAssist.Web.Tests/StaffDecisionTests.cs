using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>Authorization decisions and runner assignment (D9b): rules alone, then against SQLite.</summary>
public sealed class StaffDecisionTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-17T09:00:00+08:00");
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.Date);

    private async Task<WorkflowService> StartAsync()
    {
        var clock = new TestClock(Now);
        var service = new WorkflowService(_db, new MockPortalStore(clock), clock, NullLogger<WorkflowService>.Instance);
        await service.InitializeAsync();
        return service;
    }

    private static readonly Guid Lia = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a3");

    // ---- Rules ----

    [Fact]
    public void A_parents_request_needs_a_patient_under_18()
    {
        Assert.Null(StaffDecisionRules.ApproveProblem(AuthorizationRequestStatus.Pending, AuthorizationBasis.ParentOfMinor, Today.AddYears(-12), Today));
        Assert.NotNull(StaffDecisionRules.ApproveProblem(AuthorizationRequestStatus.Pending, AuthorizationBasis.ParentOfMinor, Today.AddYears(-30), Today));
        Assert.Null(StaffDecisionRules.ApproveProblem(AuthorizationRequestStatus.Pending, AuthorizationBasis.SignedLetter, Today.AddYears(-30), Today));
        Assert.NotNull(StaffDecisionRules.ApproveProblem(AuthorizationRequestStatus.Approved, AuthorizationBasis.SignedLetter, null, Today));
    }

    [Fact]
    public void A_runner_is_assigned_until_they_accept()
    {
        Assert.Null(StaffDecisionRules.AssignProblem(RunnerTaskStatus.PendingAssignment, null, "Ramil Ocampo"));
        Assert.Null(StaffDecisionRules.AssignProblem(RunnerTaskStatus.Assigned, "Paolo Dizon", "Ramil Ocampo"));
        Assert.NotNull(StaffDecisionRules.AssignProblem(RunnerTaskStatus.Assigned, "Paolo Dizon", "Paolo Dizon"));
        Assert.NotNull(StaffDecisionRules.AssignProblem(RunnerTaskStatus.Declined, "Liza Manalo", "Liza Manalo"));
        Assert.NotNull(StaffDecisionRules.AssignProblem(RunnerTaskStatus.Accepted, "Paolo Dizon", "Ramil Ocampo"));
        Assert.NotNull(StaffDecisionRules.AssignProblem(RunnerTaskStatus.PendingAssignment, null, "Someone Real"));
    }

    // ---- Shared store ----

    [Fact]
    public async Task Approving_an_authorization_lets_the_review_start()
    {
        var workflow = await StartAsync();
        Assert.True(workflow.AuthorizationPending(Lia));
        Assert.False((await workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00395", 0)).Succeeded);

        Assert.True((await workflow.DecideAuthorizationAsync(TestActors.Admin, "auth-lia", 0, approve: true, note: null)).Succeeded);

        Assert.False(workflow.AuthorizationPending(Lia));
        Assert.Equal(AuthorizationRequestStatus.Approved, workflow.LatestAuthorization(Lia)!.Request.Status);
        Assert.Contains(workflow.State.Notifications, n => n.Title == "Authorization approved" && n.Href == "/patient/patients/lia");
        Assert.True((await workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00395", 0)).Succeeded);

        var restarted = await StartAsync();
        Assert.Equal(AuthorizationRequestStatus.Approved, restarted.FindAuthorization("auth-lia")!.Request.Status);
    }

    [Fact]
    public async Task A_rejected_request_needs_a_note_and_can_be_asked_again()
    {
        var workflow = await StartAsync();
        Assert.False((await workflow.DecideAuthorizationAsync(TestActors.Admin, "auth-lia", 0, approve: false, note: "No")).Succeeded);
        Assert.True((await workflow.DecideAuthorizationAsync(TestActors.Admin, "auth-lia", 0, approve: false, note: "The birth certificate is unreadable.")).Succeeded);
        Assert.False((await workflow.DecideAuthorizationAsync(TestActors.Admin, "auth-lia", 0, approve: true, note: null)).Succeeded); // stale

        var again = new AuthorizationRequest("auth-new", Lia, AuthorizationBasis.ParentOfMinor, "birth-certificate.pdf", "application/pdf", 1000, Now);
        var notice = new PortalNotification("n-auth-new", "Authorization requested", "Sent", Now, false, "/patient/patients/lia");
        Assert.True(await workflow.RequestAuthorizationAsync(TestActors.Patient, new SharedAuthorization(again, "Lia Santos", "Daughter", Today.AddYears(-13), "/patient/patients/lia", 0), notice));
        Assert.True(workflow.AuthorizationPending(Lia));

        // A second request while this one waits is refused (another browser asking at the same time).
        Assert.False(await workflow.RequestAuthorizationAsync(TestActors.Patient,
            new SharedAuthorization(again with { Id = "auth-dup" }, "Lia Santos", "Daughter", Today.AddYears(-13), "/patient/patients/lia", 0), notice));
    }

    [Fact]
    public async Task Assigning_a_runner_never_changes_the_application()
    {
        var workflow = await StartAsync();
        var before = workflow.Find("MA-2026-00452")!.Application.Status;

        Assert.True((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-31", 0, "Ramil Ocampo")).Succeeded);

        var assigned = workflow.FindRunnerRequest("rr-31")!;
        Assert.Equal(RunnerTaskStatus.Assigned, assigned.Request.Status);
        Assert.Equal("Ramil Ocampo", assigned.Request.RunnerName);
        Assert.Equal(before, workflow.Find("MA-2026-00452")!.Application.Status);
        Assert.Empty(workflow.State.Notifications); // MA-2026-00452 belongs to another account

        Assert.False((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-27", 0, "Liza Manalo")).Succeeded); // she declined it
        Assert.True((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-27", 0, "Cora Bautista")).Succeeded);
        Assert.Contains(workflow.State.Notifications, n => n.Title == "Runner assigned");
        Assert.False((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-29", 0, "Ramil Ocampo")).Succeeded); // already accepted
    }

    [Fact]
    public async Task A_family_runner_request_is_one_per_application_and_cancellable_before_assignment()
    {
        var workflow = await StartAsync();
        var request = new RunnerRequestItem("rr-family", "MA-2026-00418", "Submit a corrected document", "San Isidro General Hospital",
            Now.AddDays(2), RunnerTaskStatus.PendingAssignment, null, RunnerErrand.SubmitCorrectedDocument, RequestedAt: Now);
        var notice = new PortalNotification("n-rr-family", "Runner help requested", "Sent", Now, false, "/patient/runner");

        Assert.True(await workflow.RequestRunnerAsync(TestActors.Patient, request, notice));
        Assert.False(await workflow.RequestRunnerAsync(TestActors.Patient, request with { Id = "rr-family-2" }, notice));
        Assert.True(workflow.IsFamilyRunnerRequest("rr-family"));
        Assert.False(workflow.IsFamilyRunnerRequest("rr-31"));

        Assert.False(await workflow.CancelRunnerRequestAsync(TestActors.Patient, "rr-31", _ => true)); // seeded, not the family's
        Assert.True(await workflow.CancelRunnerRequestAsync(TestActors.Patient, "rr-family", _ => true));
        Assert.Equal(RunnerTaskStatus.Cancelled, workflow.FindRunnerRequest("rr-family")!.Request.Status);
    }

    [Fact]
    public async Task A_database_from_D9a_gets_the_new_tables_on_start()
    {
        await StartAsync();
        await using (var db = _db.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"Authorizations\"; DROP TABLE \"RunnerRequests\";");

        var workflow = await StartAsync();
        Assert.True((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-31", 0, "Ramil Ocampo")).Succeeded);
    }

    public void Dispose() => _db.Dispose();
}
