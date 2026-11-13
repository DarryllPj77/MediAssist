namespace MediAssist.Web.Models;

// Optional runner requests from the patient portal (Milestone 2, Phase D6). Phase 1 rules: runner
// help is optional, covers permitted errands only, and the runner's status never changes the
// application's status. Assignment is D9 (administrator); doing the errand is D10 (runner).

/// <summary>The errands a patient may ask a runner to do.</summary>
public enum RunnerErrand
{
    DeliverDocuments,
    SubmitCorrectedDocument,
    PickUpDocument,
    ReturnSignedForm
}

/// <summary>Labels, limits and validation for runner requests.</summary>
public static class RunnerRules
{
    public const int NotesMaxLength = 200;
    public const int LatestDays = 30;

    public static IReadOnlyList<string> Windows { get; } = ["Morning, 8:00 AM to 12 noon", "Afternoon, 1:00 PM to 5:00 PM"];

    /// <summary>Statuses an application can be in for runner help (submitted and not finished).</summary>
    public static bool IsEligible(ApplicationStatus status) => status is
        ApplicationStatus.Submitted or ApplicationStatus.UnderReview or ApplicationStatus.NeedsCorrection or ApplicationStatus.ReadyForProcessing;

    /// <summary>A request still in progress (not completed, declined or cancelled).</summary>
    public static bool IsOpen(RunnerTaskStatus status) => status is
        RunnerTaskStatus.Requested or RunnerTaskStatus.PendingAssignment or RunnerTaskStatus.Assigned
        or RunnerTaskStatus.Accepted or RunnerTaskStatus.InProgress;

    public static string ErrandLabel(RunnerErrand errand) => errand switch
    {
        RunnerErrand.DeliverDocuments => "Deliver documents to the office",
        RunnerErrand.SubmitCorrectedDocument => "Submit a corrected document",
        RunnerErrand.PickUpDocument => "Pick up a document or voucher",
        RunnerErrand.ReturnSignedForm => "Return a signed form",
        _ => errand.ToString()
    };

    public static string ErrandHint(RunnerErrand errand) => errand switch
    {
        RunnerErrand.DeliverDocuments => "Bring the papers you prepared to the hospital's social service office.",
        RunnerErrand.SubmitCorrectedDocument => "Hand in a corrected copy a reviewer asked for.",
        RunnerErrand.PickUpDocument => "Collect a release, voucher or result the office has ready.",
        RunnerErrand.ReturnSignedForm => "Return an acknowledgement or form you have signed.",
        _ => ""
    };

    /// <summary>What runners never do (shown before the request).</summary>
    public static IReadOnlyList<string> NotAllowed { get; } =
    [
        "Handle cash or make payments",
        "Buy or carry medicine",
        "Make medical or financial decisions for you",
    ];

    /// <summary>Field errors keyed "application", "errand", "date", "window", "notes", "confirm".</summary>
    public static Dictionary<string, string> Validate(AssistanceApplication? application, bool hasOpenRequest, RunnerErrand? errand,
        DateOnly? date, string? window, string? notes, bool confirmed, DateOnly today)
    {
        var errors = new Dictionary<string, string>();
        if (application is null) errors["application"] = "Choose the application this errand is for.";
        else if (!IsEligible(application.Status)) errors["application"] = "Runner help is available for submitted applications that are not finished.";
        else if (hasOpenRequest) errors["application"] = "This application already has a runner request in progress.";

        if (errand is null) errors["errand"] = "Choose what the runner should do.";

        if (date is not { } d) errors["date"] = "Choose the date you need it done.";
        else if (d <= today) errors["date"] = "Choose a date from tomorrow onwards.";
        else if (d > today.AddDays(LatestDays)) errors["date"] = $"Choose a date within the next {LatestDays} days.";

        if (!Windows.Contains(window ?? "")) errors["window"] = "Choose a time of day.";
        if ((notes?.Trim().Length ?? 0) > NotesMaxLength) errors["notes"] = $"Keep notes to {NotesMaxLength} characters or fewer.";
        if (!confirmed) errors["confirm"] = "Confirm that you understand runner help is optional.";
        return errors;
    }
}
