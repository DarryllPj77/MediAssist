using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>Data ownership (M3.5): each patient account sees and changes only its own household and records.</summary>
public sealed class OwnershipTests
{
    private static WizardDraft Draft(ApplicationWorkspace browser, Guid patient, DateTimeOffset now) =>
        new(browser.NewDraftId(), patient, "Bayview Medical Center", "medicine-cost", 5, true, now, now);

    [Fact]
    public async Task A_new_patient_account_starts_with_only_itself()
    {
        using var h = await TestHarness.StartAsync();
        var jun = await h.BrowserAsync(account: TestActors.OtherPatient);

        var self = Assert.Single(jun.Household);
        Assert.Equal(("Jun Dela Cruz", AuthorizationStatus.Self), (self.FullName, self.Authorization));
        Assert.Empty(jun.Submitted);
        Assert.Empty(jun.Notifications);
        Assert.Empty(new MockPatientPortalDataService(h.Store, jun, new StubAccountService(h.Store)).GetApplicationsAsync().Result);
    }

    [Fact]
    public async Task Two_families_never_see_each_others_records()
    {
        using var h = await TestHarness.StartAsync();
        var maria = await h.BrowserAsync();
        var jun = await h.BrowserAsync(account: TestActors.OtherPatient);

        var junSelf = jun.Household.Single().Id;
        var draft = await jun.SaveDraftAsync(Draft(jun, junSelf, h.Clock.Now));
        var reference = await jun.SubmitAsync(draft);

        Assert.NotNull(reference);
        Assert.Equal(TestActors.OtherPatient.UserId, h.Workflow.Find(reference!)!.OwnerId);
        Assert.Contains(jun.Submitted, a => a.Application.ReferenceNumber == reference);
        Assert.DoesNotContain(maria.Submitted, a => a.Application.ReferenceNumber == reference);
        Assert.Contains(jun.Notifications, n => n.Id == $"n-{reference}");
        Assert.DoesNotContain(maria.Notifications, n => n.Id == $"n-{reference}");

        // Seeded records are Maria's: Jun can't upload to them, ask a runner for them, or cancel her requests.
        var scan = SimulatedFiles.SamplesFor("Clinical abstract")[0];
        Assert.False((await h.Workflow.UploadAsync(TestActors.OtherPatient, "MA-2026-00418", "Clinical abstract", scan)).Succeeded);
        var request = new RunnerRequestItem("rr-jun", "MA-2026-00418", "Deliver documents", "San Isidro General Hospital",
            h.Clock.Now.AddDays(2), RunnerTaskStatus.PendingAssignment, null, RequestedAt: h.Clock.Now);
        Assert.False(await h.Workflow.RequestRunnerAsync(TestActors.OtherPatient, request, new("n-rr-jun", "t", "m", h.Clock.Now, false)));
        Assert.True((await h.Workflow.UploadAsync(TestActors.Patient, "MA-2026-00418", "Clinical abstract", scan)).Succeeded);
        Assert.Equal(TestActors.Patient.UserId, h.Workflow.Find("MA-2026-00418")!.OwnerId); // an update keeps the owner
    }

    [Fact]
    public async Task Each_account_keeps_its_own_saved_workspace_on_the_server()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mediassist-ws-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MediAssistIdentityDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        try
        {
            await using (var db = new MediAssistIdentityDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Users.AddRange(new ApplicationUser { Id = "user-maria", UserName = "m" }, new ApplicationUser { Id = "user-jun", UserName = "j" });
                await db.SaveChangesAsync();
            }
            var factory = new Factory(options);
            ServerWorkspaceStorage StorageFor(Actor a) => new(factory, new FixedActor(a), new TestClock(), NullLogger<ServerWorkspaceStorage>.Instance);

            var draft = new WizardDraft("D-AAAAAA", null, null, null, 1, false, TestClock.Default, TestClock.Default);
            Assert.True(await StorageFor(TestActors.Patient).SaveAsync(WorkspaceSnapshot.Empty with { Drafts = [draft] }));

            Assert.Single((await StorageFor(TestActors.Patient).LoadAsync())!.Drafts);   // another "browser", same account
            Assert.Null(await StorageFor(TestActors.OtherPatient).LoadAsync());           // another account sees nothing
            Assert.False(await StorageFor(TestActors.Admin).SaveAsync(WorkspaceSnapshot.Empty)); // staff have no patient workspace
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class Factory(DbContextOptions<MediAssistIdentityDbContext> options) : IDbContextFactory<MediAssistIdentityDbContext>
    {
        public MediAssistIdentityDbContext CreateDbContext() => new(options);
    }
}
