using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>Runner requests ending with their application, the audit log and retention (M3.7).</summary>
public sealed class HardeningTests
{
    [Fact]
    public async Task Completing_an_application_closes_its_open_runner_requests()
    {
        using var h = await TestHarness.StartAsync();
        var now = h.Clock.GetLocalNow();
        var request = new RunnerRequestItem("rr-maria", "MA-2026-00362", "Deliver laboratory results", "Bayview Medical Center",
            now.AddDays(1), RunnerTaskStatus.PendingAssignment, null, RunnerErrand.SubmitCorrectedDocument, RequestedAt: now);
        var notice = new PortalNotification("n-rr-maria", "Runner help requested", "Sent", now, false, "/patient/runner");
        Assert.True(await h.Workflow.RequestRunnerAsync(TestActors.Patient, request, notice));

        Assert.True((await h.Workflow.CompleteAsync(TestActors.Admin, "MA-2026-00362", 0)).Succeeded);

        var closed = h.Workflow.FindRunnerRequest("rr-maria")!.Request;
        Assert.Equal(RunnerTaskStatus.Cancelled, closed.Status);
        Assert.NotNull(closed.ClosedAt);
        Assert.Equal(RunnerTaskStatus.Completed, h.Workflow.FindRunnerRequest("rr-24")!.Request.Status); // finished errands stay finished
        Assert.False(await h.Workflow.RequestRunnerAsync(TestActors.Patient, request with { Id = "rr-maria-2" }, notice with { Id = "n-2" }));

        var restarted = await h.RestartAsync();
        Assert.Equal(RunnerTaskStatus.Cancelled, restarted.FindRunnerRequest("rr-maria")!.Request.Status);
    }

    [Fact]
    public async Task Every_saved_change_is_in_the_audit_log_with_who_made_it()
    {
        using var h = await TestHarness.StartAsync();
        Assert.True((await h.Workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded);
        Assert.False((await h.Workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded); // stale: nothing saved
        Assert.True((await h.Workflow.AssignRunnerAsync(TestActors.Admin, "rr-31", 0, "Ramil Ocampo")).Succeeded);

        await using var db = h.Database.CreateDbContext();
        var entries = await db.AuditEntries.OrderBy(e => e.Id).ToListAsync();
        Assert.Collection(entries,
            e => Assert.Equal(("Review started", "MA-2026-00452", TestActors.Admin.UserId, Roles.Administrator), (e.Action, e.Target, e.ActorId, e.Role)),
            e => Assert.Equal(("Assigned Ramil Ocampo", "rr-31"), (e.Action, e.Target)));
    }

    [Fact]
    public async Task Old_notifications_and_audit_entries_are_removed_on_start()
    {
        using var h = await TestHarness.StartAsync();
        await using (var db = h.Database.CreateDbContext())
        {
            var old = h.Clock.GetUtcNow().AddDays(-40);
            db.Notifications.Add(new StoredNotification
            {
                Id = "n-old",
                Json = System.Text.Json.JsonSerializer.Serialize(new PortalNotification("n-old", "Old", "Old", old, true),
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
            });
            db.AuditEntries.Add(new StoredAuditEntry { AtUtc = old.UtcDateTime, ActorId = "x", Action = "Old", Target = "x" });
            db.AuditEntries.Add(new StoredAuditEntry { AtUtc = h.Clock.GetUtcNow().UtcDateTime, ActorId = "x", Action = "New", Target = "x" });
            await db.SaveChangesAsync();
        }

        var restarted = new WorkflowService(h.Database, h.Store, h.Clock, NullLogger<WorkflowService>.Instance, new WorkflowOptions(30, 30));
        await restarted.InitializeAsync();

        Assert.DoesNotContain(restarted.State.Notifications, n => n.Id == "n-old");
        await using var check = h.Database.CreateDbContext();
        Assert.Equal(new[] { "New" }, await check.AuditEntries.Select(e => e.Action).ToListAsync());
    }
}
