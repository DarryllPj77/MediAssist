using MediAssist.Web.Models;
using MediAssist.Web.Services;

namespace MediAssist.Web.Tests;

/// <summary>
/// The patient workspace and patient data service (M3.3): submitting drafts, save failures in the
/// browser and in the shared store, the account/profile link, older saved data, and scoping.
/// </summary>
public sealed class WorkspaceTests
{
    private static readonly Guid Maria = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a1");

    private static WizardDraft Draft(ApplicationWorkspace browser, DateTimeOffset now) =>
        new(browser.NewDraftId(), Maria, "Bayview Medical Center", "medicine-cost", 5, true, now, now);

    private static SampleFile Scan(string document) => SimulatedFiles.SamplesFor(document)[0];

    [Fact]
    public async Task Submitting_a_draft_moves_it_and_its_uploads_to_the_shared_store()
    {
        using var h = await TestHarness.StartAsync();
        var storage = new MemoryBrowserStorage();
        var browser = await h.BrowserAsync(storage);
        var draft = await browser.SaveDraftAsync(Draft(browser, h.Clock.Now));
        Assert.True((await browser.UploadAsync(draft.Id, "Prescription", Scan("Prescription"))).Succeeded);

        var reference = await browser.SubmitAsync(draft);

        Assert.NotNull(reference);
        Assert.Empty(browser.Drafts);
        Assert.Empty(storage.Saved!.Drafts);
        Assert.Empty(storage.Saved.Uploads!);
        Assert.Single(h.Workflow.State.Uploads, u => u.ApplicationReference == reference && u.DocumentName == "Prescription");
        Assert.Equal(ApplicationStatus.Submitted, h.Workflow.Find(reference!)!.Application.Status);
        Assert.Contains(browser.Notifications, n => n.Id == $"n-{reference}" && n.PatientId == Maria);
    }

    [Fact]
    public async Task A_failed_shared_save_keeps_the_draft()
    {
        using var h = await TestHarness.StartAsync();
        var browser = await h.BrowserAsync();
        var draft = await browser.SaveDraftAsync(Draft(browser, h.Clock.Now));

        h.Database.Fail = true;
        Assert.Null(await browser.SubmitAsync(draft));

        Assert.Single(browser.Drafts);
        Assert.Empty(h.Workflow.State.Applications);
    }

    [Fact]
    public async Task A_failed_browser_save_undoes_a_draft_upload()
    {
        using var h = await TestHarness.StartAsync();
        var storage = new MemoryBrowserStorage();
        var browser = await h.BrowserAsync(storage);
        var draft = await browser.SaveDraftAsync(Draft(browser, h.Clock.Now));

        storage.FailSaves = true;
        var result = await browser.UploadAsync(draft.Id, "Prescription", Scan("Prescription"));

        Assert.False(result.Succeeded);
        Assert.Empty(browser.Uploads);
        Assert.True(browser.LastSaveFailed);
    }

    [Fact]
    public async Task A_failed_shared_save_changes_nothing_and_the_next_try_works()
    {
        using var h = await TestHarness.StartAsync();

        h.Database.Fail = true;
        var failed = await h.Workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0);
        Assert.False(failed.Succeeded);
        Assert.Equal(0, h.Workflow.Find("MA-2026-00452")!.Version);
        Assert.Equal(ApplicationStatus.Submitted, h.Workflow.Find("MA-2026-00452")!.Application.Status);

        h.Database.Fail = false;
        Assert.True((await h.Workflow.StartReviewAsync(TestActors.Admin, "MA-2026-00452", 0)).Succeeded);
    }

    [Fact]
    public async Task Submissions_at_the_same_moment_get_different_references()
    {
        using var h = await TestHarness.StartAsync();
        var browsers = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => h.BrowserAsync()));
        var drafts = await Task.WhenAll(browsers.Select(b => b.SaveDraftAsync(Draft(b, h.Clock.Now))));

        var references = await Task.WhenAll(browsers.Select((b, i) => b.SubmitAsync(drafts[i])));

        Assert.All(references, Assert.NotNull);
        Assert.Equal(references.Length, references.Distinct().Count());
        var restarted = await h.RestartAsync();
        Assert.All(references, r => Assert.NotNull(restarted.Find(r!)));
    }

    [Fact]
    public async Task The_you_profile_follows_the_account_name()
    {
        using var h = await TestHarness.StartAsync();
        var browser = await h.BrowserAsync();

        Assert.True(await browser.SyncSelfNameAsync("Mariana Santos-Cruz"));
        var self = browser.Household.Single(p => p.Authorization == AuthorizationStatus.Self);
        Assert.Equal("Mariana Santos-Cruz", self.FullName);

        // Editing the "You" profile can't change the name; the account does.
        var edited = await browser.SaveProfileAsync(self.Id, "Someone Else", null, self.DateOfBirth, "Pasig City");
        Assert.True(edited.Succeeded);
        Assert.Equal("Mariana Santos-Cruz", edited.Profile!.FullName);

        // Clearing this browser keeps the account's name on the "You" profile.
        Assert.True(await browser.ClearAsync());
        Assert.Equal("Mariana Santos-Cruz", browser.Household.Single(p => p.Authorization == AuthorizationStatus.Self).FullName);
    }

    [Fact]
    public async Task Older_saved_browser_data_still_loads()
    {
        using var h = await TestHarness.StartAsync();
        var storage = new MemoryBrowserStorage { Saved = new WorkspaceSnapshot(1, [], [], []) };
        var browser = await h.BrowserAsync(storage);

        Assert.True(browser.IsLoaded);
        Assert.Empty(browser.Uploads);
        Assert.Equal(3, browser.Household.Count);
    }

    [Fact]
    public async Task The_patient_portal_never_shows_another_accounts_application()
    {
        using var h = await TestHarness.StartAsync();
        var data = new MockPatientPortalDataService(h.Store, await h.BrowserAsync(), new StubAccountService(h.Store));

        var applications = await data.GetApplicationsAsync();

        Assert.Contains(applications, a => a.ReferenceNumber == "MA-2026-00418");
        Assert.DoesNotContain(applications, a => a.ReferenceNumber == "MA-2026-00452"); // Ramon, another account
        Assert.Null(await data.GetApplicationAsync("MA-2026-00452"));
        Assert.Empty(await data.GetApplicationHistoryAsync("MA-2026-00452"));
    }
}
