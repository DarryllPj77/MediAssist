using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>
/// M3.2 regressions, through the patient workspace: two browsers share one store, so each test
/// builds two workspaces (each with its own in-memory "browser storage") over one SQLite file.
/// </summary>
public sealed class FamilyScopeTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-25T09:00:00+08:00");
    private static readonly Guid Lia = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a3");

    private readonly TestClock _clock = new(Now);
    private MockPortalStore? _store;
    private WorkflowService? _workflow;

    private async Task<WorkflowService> WorkflowAsync()
    {
        _store ??= new MockPortalStore(_clock);
        if (_workflow is null)
        {
            _workflow = new WorkflowService(_db, _store, _clock, NullLogger<WorkflowService>.Instance);
            await _workflow.InitializeAsync();
        }
        return _workflow;
    }

    private async Task<ApplicationWorkspace> BrowserAsync()
    {
        var workflow = await WorkflowAsync();
        var workspace = new ApplicationWorkspace(new MemoryBrowserStorage(), _store!, workflow, _clock);
        await workspace.EnsureLoadedAsync();
        return workspace;
    }

    /// <summary>Add a child and ask to act for them as their parent.</summary>
    private static async Task<PatientProfile> AddChildWithRequestAsync(ApplicationWorkspace browser, string name)
    {
        var added = await browser.SaveProfileAsync(null, name, "Daughter", new DateOnly(2015, 3, 10), null);
        Assert.True(added.Succeeded);
        var file = SimulatedFiles.SamplesFor(ProfileRules.BasisDocument(AuthorizationBasis.ParentOfMinor))[0];
        var asked = await browser.RequestAuthorizationAsync(added.Profile!.Id, AuthorizationBasis.ParentOfMinor, file, confirmed: true);
        Assert.True(asked.Succeeded, string.Join("; ", asked.Errors.Values) + asked.SaveError);
        return added.Profile;
    }

    [Fact]
    public async Task F1_a_family_sees_only_notifications_about_its_own_patients()
    {
        var browserA = await BrowserAsync();
        var browserB = await BrowserAsync();
        var ana = await AddChildWithRequestAsync(browserB, "Ana Reyes");

        Assert.Contains(browserB.Notifications, n => n.Message.Contains("Ana Reyes"));
        Assert.DoesNotContain(browserA.Notifications, n => n.Message.Contains("Ana Reyes"));

        // The office's decision reaches only browser B as well.
        var workflow = await WorkflowAsync();
        var request = workflow.AuthorizationsFor(ana.Id).Single();
        Assert.True((await workflow.DecideAuthorizationAsync(request.Id, request.Version, approve: true, note: null)).Succeeded);
        Assert.Contains(browserB.Notifications, n => n.Title == "Authorization approved");
        Assert.DoesNotContain(browserA.Notifications, n => n.Title == "Authorization approved");
    }

    [Fact]
    public async Task F2_a_rejected_authorization_blocks_review_and_runner_help()
    {
        var browser = await BrowserAsync();
        var workflow = await WorkflowAsync();
        Assert.True((await workflow.DecideAuthorizationAsync("auth-lia", 0, approve: false, note: "The birth certificate is unreadable.")).Succeeded);
        Assert.Equal(AuthorizationStanding.Rejected, workflow.Authorization(Lia));

        var start = await workflow.StartReviewAsync("MA-2026-00395", 0);
        Assert.False(start.Succeeded);
        Assert.Contains("wasn't approved", start.Error);

        var assign = await workflow.AssignRunnerAsync("rr-27", 0, "Cora Bautista");
        Assert.False(assign.Succeeded);

        var family = await browser.RequestRunnerAsync("MA-2026-00395", RunnerErrand.DeliverDocuments,
            DateOnly.FromDateTime(Now.Date).AddDays(3), RunnerRules.Windows[0], null, confirmed: true);
        Assert.False(family.Succeeded);
        Assert.Contains("wasn't approved", family.Errors["application"]);

        // Once a new request is approved, the review can start.
        var file = SimulatedFiles.SamplesFor("Birth certificate")[0];
        Assert.True((await browser.RequestAuthorizationAsync(Lia, AuthorizationBasis.ParentOfMinor, file, confirmed: true)).Succeeded);
        var again = workflow.LatestAuthorization(Lia)!;
        Assert.True((await workflow.DecideAuthorizationAsync(again.Id, again.Version, approve: true, note: null)).Succeeded);
        Assert.True((await workflow.StartReviewAsync("MA-2026-00395", 0)).Succeeded);
    }

    [Fact]
    public async Task F3_clearing_keeps_added_patients_that_have_shared_records()
    {
        var browser = await BrowserAsync();
        var ana = await AddChildWithRequestAsync(browser, "Ana Reyes");
        var noRecords = (await browser.SaveProfileAsync(null, "Ben Reyes", "Son", new DateOnly(2016, 1, 5), null)).Profile!;

        Assert.True(await browser.ClearAsync());

        Assert.Contains(browser.Household, p => p.Id == ana.Id);
        Assert.DoesNotContain(browser.Household, p => p.Id == noRecords.Id);
        Assert.Equal(AuthorizationStatus.PendingReview, browser.Household.Single(p => p.Id == ana.Id).Authorization);
    }

    public void Dispose() => _db.Dispose();
}
