using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>Runner steps (D10): the rules alone, then whole errands against SQLite.</summary>
public sealed class RunnerStepTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-17T09:00:00+08:00");

    private async Task<WorkflowService> StartAsync()
    {
        var clock = new TestClock(Now);
        var service = new WorkflowService(_db, new MockPortalStore(clock) { SeedOwnerId = TestActors.Patient.UserId }, clock, NullLogger<WorkflowService>.Instance);
        await service.InitializeAsync();
        return service;
    }

    private static RunnerRequestItem Request(RunnerTaskStatus status, string? runner = "Ramil Ocampo") =>
        new("rr-x", "MA-2026-00418", "Deliver documents", "San Isidro General Hospital", Now.AddDays(1), status, runner);

    private const string Reason = "My motorbike broke down this morning.";

    // ---- Rules ----

    [Theory]
    [InlineData(RunnerTaskStatus.Assigned, RunnerStepKind.Accepted, true)]
    [InlineData(RunnerTaskStatus.Assigned, RunnerStepKind.Started, false)]
    [InlineData(RunnerTaskStatus.Accepted, RunnerStepKind.Started, true)]
    [InlineData(RunnerTaskStatus.Accepted, RunnerStepKind.Completed, false)]
    [InlineData(RunnerTaskStatus.InProgress, RunnerStepKind.Completed, true)]
    [InlineData(RunnerTaskStatus.InProgress, RunnerStepKind.HandedBack, true)]
    [InlineData(RunnerTaskStatus.Assigned, RunnerStepKind.HandedBack, false)]
    [InlineData(RunnerTaskStatus.Completed, RunnerStepKind.HandedBack, false)]
    public void Steps_follow_the_errand(RunnerTaskStatus status, RunnerStepKind kind, bool allowed) =>
        Assert.Equal(allowed, RunnerRules.StepProblem(Request(status), "Ramil Ocampo", kind, Reason) is null);

    [Fact]
    public void Only_the_assigned_runner_acts_and_a_hand_back_needs_a_reason()
    {
        Assert.NotNull(RunnerRules.StepProblem(Request(RunnerTaskStatus.Assigned), "Cora Bautista", RunnerStepKind.Accepted, null));
        Assert.NotNull(RunnerRules.StepProblem(Request(RunnerTaskStatus.Assigned), "Ramil Ocampo", RunnerStepKind.Declined, null));
        Assert.NotNull(RunnerRules.StepProblem(Request(RunnerTaskStatus.Assigned), "Ramil Ocampo", RunnerStepKind.Declined, "Busy"));
        Assert.Null(RunnerRules.StepProblem(Request(RunnerTaskStatus.InProgress), "Ramil Ocampo", RunnerStepKind.Completed, null));
    }

    // ---- Whole errands ----

    [Fact]
    public async Task A_decline_returns_the_errand_and_the_next_runner_finishes_it()
    {
        var workflow = await StartAsync();
        var appStatus = workflow.Find("MA-2026-00395")!.Application.Status;
        int V() => workflow.FindRunnerRequest("rr-27")!.Version;

        // rr-27 was declined by Liza (seeded); the office gives it to Ramil, who declines too.
        Assert.True((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-27", V(), "Ramil Ocampo")).Succeeded);
        Assert.False((await workflow.RunnerStepAsync(TestActors.Runner("Cora Bautista"), "rr-27", V(), RunnerStepKind.Accepted, null)).Succeeded);
        Assert.True((await workflow.RunnerStepAsync(TestActors.Runner("Ramil Ocampo"), "rr-27", V(), RunnerStepKind.Declined, Reason)).Succeeded);

        var declined = workflow.FindRunnerRequest("rr-27")!.Request;
        Assert.Equal(RunnerTaskStatus.Declined, declined.Status);
        Assert.Equal(Reason, declined.HandBackReason);
        Assert.NotNull(StaffDecisionRules.AssignProblem(declined.Status, declined.RunnerName, "Ramil Ocampo"));
        Assert.Contains(workflow.State.Notifications, n => n.Title == "Finding another runner");

        Assert.True((await workflow.AssignRunnerAsync(TestActors.Admin, "rr-27", V(), "Cora Bautista")).Succeeded);
        Assert.True((await workflow.RunnerStepAsync(TestActors.Runner("Cora Bautista"), "rr-27", V(), RunnerStepKind.Accepted, null)).Succeeded);
        Assert.True((await workflow.RunnerStepAsync(TestActors.Runner("Cora Bautista"), "rr-27", V(), RunnerStepKind.Started, null)).Succeeded);
        Assert.True((await workflow.RunnerStepAsync(TestActors.Runner("Cora Bautista"), "rr-27", V(), RunnerStepKind.Completed, "Left with the cashier.")).Succeeded);

        var done = workflow.FindRunnerRequest("rr-27")!.Request;
        Assert.Equal(RunnerTaskStatus.Completed, done.Status);
        Assert.Equal("Left with the cashier.", done.CompletionNote);
        Assert.Equal(appStatus, workflow.Find("MA-2026-00395")!.Application.Status);
        Assert.Contains(workflow.State.Notifications, n => n.Title == "Runner errand done");

        // Every runner who had it keeps it in their history; the steps survive a restart.
        Assert.Contains(workflow.RunnerTasksFor("Ramil Ocampo"), t => t.Id == "rr-27");
        Assert.Contains(workflow.RunnerTasksFor("Liza Manalo"), t => t.Id == "rr-27");
        var restarted = await StartAsync();
        Assert.Equal(8, restarted.FindRunnerRequest("rr-27")!.Request.Steps!.Count);
    }

    [Fact]
    public async Task A_hand_back_after_accepting_returns_it_and_a_stale_step_is_refused()
    {
        var workflow = await StartAsync();
        var seen = workflow.FindRunnerRequest("rr-29")!.Version; // Paolo accepted (seeded)

        Assert.True((await workflow.RunnerStepAsync(TestActors.Runner("Paolo Dizon"), "rr-29", seen, RunnerStepKind.Started, null)).Succeeded);
        var stale = await workflow.RunnerStepAsync(TestActors.Runner("Paolo Dizon"), "rr-29", seen, RunnerStepKind.HandedBack, Reason);
        Assert.True(stale.Conflict);

        Assert.True((await workflow.RunnerStepAsync(TestActors.Runner("Paolo Dizon"), "rr-29", seen + 1, RunnerStepKind.HandedBack, Reason)).Succeeded);
        Assert.Equal(RunnerTaskStatus.Declined, workflow.FindRunnerRequest("rr-29")!.Request.Status);
    }

    [Fact]
    public async Task The_runner_sees_only_what_the_errand_needs()
    {
        var workflow = await StartAsync();
        var task = workflow.RunnerTask("rr-29")!;
        Assert.Equal("Maria", task.ContactFirstName);
        Assert.False(string.IsNullOrEmpty(task.District));
        Assert.All(workflow.RunnerTasksFor("Paolo Dizon"), t => Assert.True(
            t.Request.RunnerName == "Paolo Dizon" || t.Request.Steps!.Any(s => s.Runner == "Paolo Dizon")));
    }

    public void Dispose() => _db.Dispose();
}
