using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>The shared store under migrations, tolerant loading and database-level concurrency (M3.6).</summary>
public sealed class DatabaseReadinessTests
{
    [Fact]
    public async Task A_database_made_before_migrations_is_brought_under_them_and_keeps_its_data()
    {
        using var h = await TestHarness.StartAsync();
        Assert.True((await h.Workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded);

        // Make it look like a pre-M3.6 file: the first migration's tables, no history, no later columns.
        await using (var db = h.Database.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                "DROP TABLE \"__EFMigrationsHistory\"; ALTER TABLE \"Authorizations\" DROP COLUMN \"Version\"; ALTER TABLE \"RunnerRequests\" DROP COLUMN \"Version\";");
        }

        var restarted = await h.RestartAsync();

        Assert.Equal(ApplicationStatus.UnderReview, restarted.Find("MA-2026-00452")!.Application.Status);
        await using (var db = h.Database.CreateDbContext())
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.True((await restarted.AssignRunnerAsync(TestActors.Admin, "rr-31", 0, "Ramil Ocampo")).Succeeded);
    }

    [Fact]
    public async Task One_unreadable_row_does_not_stop_the_app()
    {
        using var h = await TestHarness.StartAsync();
        await using (var db = h.Database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Applications\" (\"Reference\", \"PatientId\", \"Status\", \"Version\", \"Json\") VALUES ('MA-BROKEN', '00000000-0000-0000-0000-000000000000', 1, 1, 'not json');");

        var restarted = await h.RestartAsync();

        Assert.Null(restarted.Find("MA-BROKEN"));
        Assert.True((await restarted.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded);
    }

    [Fact]
    public async Task A_second_server_cannot_overwrite_a_newer_row()
    {
        using var h = await TestHarness.StartAsync();
        var serverA = h.Workflow;
        Assert.True((await serverA.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded); // version 1 in the database

        // Server B reads the database and moves the application on (version 2).
        var serverB = new WorkflowService(h.Database, h.Store, h.Clock, NullLogger<WorkflowService>.Instance);
        await serverB.InitializeAsync();
        var checklist = serverB.Effective(serverB.Find("MA-2026-00452")!);
        var missing = checklist.Items.First(i => i.Status == DocumentStatus.Missing).Name;
        Assert.True((await serverB.DecideDocumentAsync(TestActors.Admin, "MA-2026-00452", 1, missing, false, "Please send this document too.")).Succeeded);

        // Server A still thinks it is version 1: its write is refused, and it reloads what the database holds.
        var stale = await serverA.DecideDocumentAsync(TestActors.Admin, "MA-2026-00452", 1, missing, false, "A different reason for the same document.");

        Assert.False(stale.Succeeded);
        var reloaded = serverA.Find("MA-2026-00452")!;
        Assert.Equal(2, reloaded.Version);
        Assert.Equal("Please send this document too.", reloaded.Checklist.Items.First(i => i.Name == missing).ReviewerNote);
    }
}
