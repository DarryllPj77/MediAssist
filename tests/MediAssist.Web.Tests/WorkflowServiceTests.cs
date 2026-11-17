using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

/// <summary>The shared workflow store (D9a) against a real SQLite file, one per test.</summary>
public sealed class WorkflowServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"mediassist-test-{Guid.NewGuid():N}.db");
    private readonly FixedClock _clock = new(DateTimeOffset.Parse("2026-11-17T09:00:00+08:00"));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone { get; } =
            TimeZoneInfo.CreateCustomTimeZone("PH", TimeSpan.FromHours(8), "Philippines", "Philippines");
    }

    private sealed class Factory(string path) : IDbContextFactory<WorkflowDbContext>
    {
        public WorkflowDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<WorkflowDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options);
    }

    private async Task<WorkflowService> StartAsync()
    {
        var service = new WorkflowService(new Factory(_path), new MockPortalStore(_clock), _clock, NullLogger<WorkflowService>.Instance);
        await service.InitializeAsync();
        return service;
    }

    private static readonly Guid Maria = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a1");

    /// <summary>A two-document submission with one document uploaded and one missing.</summary>
    private Func<string, Submission> NewApplication() => reference =>
    {
        var now = _clock.GetLocalNow();
        var checklist = new ApplicationChecklist(reference, "Medicine Cost Assistance", "Maria Santos",
        [
            new("Prescription", DocumentStatus.Missing, true, "Your doctor"),
            new("Valid ID", DocumentStatus.Missing, true, "Any ID"),
        ]);
        var application = new AssistanceApplication(reference, Maria, "Maria Santos", "Bayview Medical Center",
            "Medicine Cost Assistance", ApplicationStatus.Submitted, 0, 2, now, "Add the missing document", "medicine-cost", now);
        DocumentUpload[] uploads = [new($"up-{reference}", reference, "Prescription", "rx.pdf", "application/pdf", 2000, now, 1)];
        return new Submission(new SharedApplication(application, checklist,
            [new(reference, now, ApplicationEventKind.Submitted, "Application submitted", "You", Status: ApplicationStatus.Submitted)], 0),
            uploads, new($"n-{reference}", "Application submitted", "Sent", now, false, $"/patient/applications/{reference}"));
    };

    private static SampleFile File(string name) => new(name, name, "application/pdf", 1500);

    [Fact]
    public async Task Each_submission_gets_its_own_reference_and_survives_a_restart()
    {
        var workflow = await StartAsync();
        var first = await workflow.SubmitAsync(null, NewApplication());
        var second = await workflow.SubmitAsync(null, NewApplication());

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);

        var restarted = await StartAsync();
        Assert.Equal(ApplicationStatus.Submitted, restarted.Find(first!)!.Application.Status);
        Assert.Single(restarted.State.Uploads, u => u.ApplicationReference == first);
        Assert.Contains(restarted.State.Notifications, n => n.Id == $"n-{first}");
    }

    [Fact]
    public async Task A_full_review_round_with_one_correction()
    {
        var workflow = await StartAsync();
        var reference = (await workflow.SubmitAsync(null, NewApplication()))!;
        int Version() => workflow.Find(reference)!.Version;

        Assert.True((await workflow.StartReviewAsync(reference, Version())).Succeeded);
        Assert.False((await workflow.MarkReadyAsync(reference, Version())).Succeeded); // nothing accepted yet

        Assert.True((await workflow.DecideDocumentAsync(reference, Version(), "Prescription", false, "The doctor's signature is missing.")).Succeeded);
        Assert.True((await workflow.DecideDocumentAsync(reference, Version(), "Valid ID", false, "Please send a valid government ID.")).Succeeded);
        Assert.True((await workflow.RequestCorrectionsAsync(reference, Version())).Succeeded);

        var sentBack = workflow.Find(reference)!;
        Assert.Equal(ApplicationStatus.NeedsCorrection, sentBack.Application.Status);
        Assert.Equal(UploadReviewState.Rejected, workflow.LatestUpload(reference, "Prescription")!.ReviewState);
        Assert.Contains(workflow.State.Notifications, n => n.Title == "Correction requested");

        // One new copy is not enough; the second returns the application to review.
        var first = await workflow.UploadAsync(reference, "Prescription", File("rx-signed.pdf"), _ => true);
        Assert.False(first.CorrectionSent);
        var second = await workflow.UploadAsync(reference, "Valid ID", File("id.pdf"), _ => true);
        Assert.True(second.CorrectionSent);
        Assert.Equal(ApplicationStatus.UnderReview, workflow.Find(reference)!.Application.Status);
        Assert.Equal(2, workflow.LatestUpload(reference, "Prescription")!.Version);

        Assert.True((await workflow.DecideDocumentAsync(reference, Version(), "Prescription", true, null)).Succeeded);
        Assert.True((await workflow.DecideDocumentAsync(reference, Version(), "Valid ID", true, null)).Succeeded);
        Assert.True((await workflow.MarkReadyAsync(reference, Version())).Succeeded);
        Assert.True((await workflow.CompleteAsync(reference, Version())).Succeeded);

        var done = workflow.Find(reference)!;
        Assert.Equal(ApplicationStatus.Completed, done.Application.Status);
        Assert.Equal(2, done.Application.DocumentsReady);
        Assert.Contains(done.Events, e => e.Kind == ApplicationEventKind.Completed && e.Detail!.Contains("program office"));
    }

    [Fact]
    public async Task A_stale_version_changes_nothing()
    {
        var workflow = await StartAsync();
        var reference = (await workflow.SubmitAsync(null, NewApplication()))!;
        var seen = workflow.Find(reference)!.Version;
        await workflow.StartReviewAsync(reference, seen);

        var result = await workflow.DecideDocumentAsync(reference, seen, "Prescription", true, null);

        Assert.True(result.Conflict);
        Assert.Equal(UploadReviewState.Pending, workflow.LatestUpload(reference, "Prescription")!.ReviewState);
    }

    [Fact]
    public async Task An_accepted_document_cannot_be_replaced_and_scope_is_checked()
    {
        var workflow = await StartAsync();
        var reference = (await workflow.SubmitAsync(null, NewApplication()))!;
        await workflow.StartReviewAsync(reference, workflow.Find(reference)!.Version);
        await workflow.DecideDocumentAsync(reference, workflow.Find(reference)!.Version, "Prescription", true, null);

        Assert.False((await workflow.UploadAsync(reference, "Prescription", File("again.pdf"), _ => true)).Succeeded);
        Assert.False((await workflow.UploadAsync(reference, "Valid ID", File("id.pdf"), _ => false)).Succeeded);
    }

    [Fact]
    public async Task A_seeded_application_is_copied_on_its_first_change()
    {
        var workflow = await StartAsync();
        Assert.Equal(0, workflow.Find("MA-2026-00452")!.Version);

        Assert.True((await workflow.StartReviewAsync("MA-2026-00452", 0)).Succeeded);

        var restarted = await StartAsync();
        Assert.Equal(1, restarted.Find("MA-2026-00452")!.Version);
        Assert.Equal(ApplicationStatus.UnderReview, restarted.Find("MA-2026-00452")!.Application.Status);
    }

    [Fact]
    public async Task Review_cannot_start_while_the_patients_authorization_is_pending()
    {
        var workflow = await StartAsync();
        var result = await workflow.StartReviewAsync("MA-2026-00395", 0); // Lia's authorization is being reviewed
        Assert.False(result.Succeeded);
        Assert.False(result.Conflict);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (System.IO.File.Exists(_path)) System.IO.File.Delete(_path);
    }
}
