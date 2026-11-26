using MediAssist.Web.Models;

namespace MediAssist.Web.Tests;

/// <summary>
/// The shared service checks the acting account's role and records who did what (M3.4b), so a page
/// that is bypassed still can't act for the wrong role.
/// </summary>
public sealed class ActorTests
{
    [Fact]
    public async Task Each_action_needs_its_role()
    {
        using var h = await TestHarness.StartAsync();
        var w = h.Workflow;

        Assert.False((await w.StartReviewAsync(TestActors.Patient, "MA-2026-00452", 0)).Succeeded);
        Assert.False((await w.StartReviewAsync(TestActors.Runner("Paolo Dizon"), "MA-2026-00452", 0)).Succeeded);
        Assert.False((await w.StartReviewAsync(Actor.Nobody, "MA-2026-00452", 0)).Succeeded);
        Assert.False((await w.DecideAuthorizationAsync(TestActors.Patient, "auth-lia", 0, true, null)).Succeeded);
        Assert.False((await w.AssignRunnerAsync(TestActors.Runner("Ramil Ocampo"), "rr-31", 0, "Ramil Ocampo")).Succeeded);
        Assert.False((await w.RunnerStepAsync(TestActors.Admin, "rr-29", 0, RunnerStepKind.Started, null)).Succeeded);
        Assert.False(await w.CancelRunnerRequestAsync(TestActors.Admin, "rr-31", _ => true));
        Assert.Null(await w.SubmitAsync(TestActors.Admin, null, _ => throw new InvalidOperationException("must not build")));

        // Nothing changed.
        Assert.Empty(w.State.Applications);
        Assert.Empty(w.State.Authorizations);
        Assert.Empty(w.State.RunnerRequests);
    }

    [Fact]
    public async Task Shared_actions_record_who_did_them()
    {
        using var h = await TestHarness.StartAsync();
        var w = h.Workflow;

        Assert.True((await w.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded);
        var started = w.Find("MA-2026-00452")!.Events.Last();
        Assert.Equal(("Reviewer", "user-ana", "Ana Reyes"), (started.Actor, started.ActorId, started.ActorName));

        Assert.True((await w.DecideAuthorizationAsync(TestActors.Admin, "auth-lia", 0, true, null)).Succeeded);
        Assert.Equal("Ana Reyes", w.FindAuthorization("auth-lia")!.DecidedByName);

        Assert.True((await w.AssignRunnerAsync(TestActors.Admin, "rr-31", 0, "Ramil Ocampo")).Succeeded);
        var assigned = w.FindRunnerRequest("rr-31")!.Request.Steps!.Last();
        Assert.Equal(("Ramil Ocampo", "Ana Reyes"), (assigned.Runner, assigned.ByName));

        var ramil = TestActors.Runner("Ramil Ocampo");
        Assert.True((await w.RunnerStepAsync(ramil, "rr-31", w.FindRunnerRequest("rr-31")!.Version, RunnerStepKind.Accepted, null)).Succeeded);
        Assert.Equal(ramil.UserId, w.FindRunnerRequest("rr-31")!.Request.Steps!.Last().ById);

        // Survives a restart.
        var restarted = await h.RestartAsync();
        Assert.Equal("user-ana", restarted.Find("MA-2026-00452")!.Events.Last().ActorId);
    }

    [Fact]
    public async Task A_runner_acts_only_as_themselves()
    {
        using var h = await TestHarness.StartAsync();
        // rr-29 is Paolo's; Ramil's account can't take its next step.
        var result = await h.Workflow.RunnerStepAsync(TestActors.Runner("Ramil Ocampo"), "rr-29", 0, RunnerStepKind.Started, null);
        Assert.False(result.Succeeded);
        Assert.True((await h.Workflow.RunnerStepAsync(TestActors.Runner("Paolo Dizon"), "rr-29", 0, RunnerStepKind.Started, null)).Succeeded);
    }
}
