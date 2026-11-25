using MediAssist.Web.Models;

namespace MediAssist.Web.Tests;

/// <summary>The review rules and checklist overlay (D9a), without storage.</summary>
public class ReviewRulesTests
{
    private static ChecklistItem Item(string name, DocumentStatus status) => new(name, status, true, "Anywhere");

    private static ApplicationChecklist Checklist(params ChecklistItem[] items) => new("MA-2026-00001", "Program", "Patient", items);

    [Fact]
    public void A_missing_document_can_be_asked_for_but_not_accepted()
    {
        var missing = Item("Valid ID", DocumentStatus.Missing);
        Assert.NotNull(ReviewRules.DecideProblem(ApplicationStatus.UnderReview, missing, accept: true));
        Assert.Null(ReviewRules.DecideProblem(ApplicationStatus.UnderReview, missing, accept: false));
    }

    [Fact]
    public void Documents_are_decided_only_while_in_review()
    {
        var waiting = Item("Valid ID", DocumentStatus.PendingReview);
        Assert.Null(ReviewRules.DecideProblem(ApplicationStatus.UnderReview, waiting, accept: true));
        Assert.NotNull(ReviewRules.DecideProblem(ApplicationStatus.Submitted, waiting, accept: true));
        Assert.NotNull(ReviewRules.DecideProblem(ApplicationStatus.UnderReview, Item("Valid ID", DocumentStatus.Complete), accept: true));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Too short", false)]
    [InlineData("The signature on page 2 can't be read.", true)]
    public void A_reason_must_say_what_to_fix(string? reason, bool valid) =>
        Assert.Equal(valid, ReviewRules.ReasonProblem(reason) is null);

    [Fact]
    public void A_reason_over_the_limit_is_refused() =>
        Assert.NotNull(ReviewRules.ReasonProblem(new string('a', ReviewRules.ReasonMax + 1)));

    [Fact]
    public void Corrections_need_every_copy_decided_and_one_sent_back()
    {
        var undecided = Checklist(Item("A", DocumentStatus.PendingReview), Item("B", DocumentStatus.NeedsCorrection));
        var nothingSentBack = Checklist(Item("A", DocumentStatus.Complete), Item("B", DocumentStatus.Missing));
        var ready = Checklist(Item("A", DocumentStatus.Complete), Item("B", DocumentStatus.NeedsCorrection));

        Assert.NotNull(ReviewRules.CorrectionProblem(ApplicationStatus.UnderReview, undecided));
        Assert.NotNull(ReviewRules.CorrectionProblem(ApplicationStatus.UnderReview, nothingSentBack));
        Assert.Null(ReviewRules.CorrectionProblem(ApplicationStatus.UnderReview, ready));
        Assert.NotNull(ReviewRules.CorrectionProblem(ApplicationStatus.Submitted, ready));
    }

    [Fact]
    public void Ready_for_processing_needs_every_required_document_accepted()
    {
        Assert.Equal("1 required document isn't accepted yet.",
            ReviewRules.ReadyProblem(ApplicationStatus.UnderReview, Checklist(Item("A", DocumentStatus.Complete), Item("B", DocumentStatus.PendingReview))));
        Assert.Null(ReviewRules.ReadyProblem(ApplicationStatus.UnderReview,
            Checklist(Item("A", DocumentStatus.Complete), Item("B", DocumentStatus.NotApplicable))));
    }

    [Fact]
    public void Review_waits_for_an_approved_authorization()
    {
        Assert.NotNull(ReviewRules.StartProblem(ApplicationStatus.Submitted, AuthorizationStanding.Pending));
        Assert.NotNull(ReviewRules.StartProblem(ApplicationStatus.Submitted, AuthorizationStanding.Rejected));
        Assert.Null(ReviewRules.StartProblem(ApplicationStatus.Submitted, AuthorizationStanding.Approved));
        Assert.Null(ReviewRules.StartProblem(ApplicationStatus.Submitted, AuthorizationStanding.NotNeeded));
        Assert.NotNull(ReviewRules.StartProblem(ApplicationStatus.UnderReview, AuthorizationStanding.NotNeeded));
    }

    [Fact]
    public void Only_an_undecided_latest_upload_shows_as_being_reviewed()
    {
        var checklist = Checklist(Item("A", DocumentStatus.Missing), Item("B", DocumentStatus.NeedsCorrection) with { ReviewerNote = "Blurry" });
        var at = DateTimeOffset.Parse("2026-11-17T09:00:00+08:00");
        DocumentUpload[] uploads =
        [
            new("u1", "MA-2026-00001", "A", "a.pdf", "application/pdf", 1000, at, 1),
            new("u2", "MA-2026-00001", "B", "b.pdf", "application/pdf", 1000, at, 1, ReviewState: UploadReviewState.Rejected),
        ];

        var shown = ChecklistOverlay.Apply(checklist, uploads);

        Assert.Equal(DocumentStatus.PendingReview, shown.Items[0].Status);
        Assert.Equal(DocumentStatus.NeedsCorrection, shown.Items[1].Status);
        Assert.Equal("Blurry", shown.Items[1].ReviewerNote);
    }
}
