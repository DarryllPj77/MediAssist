namespace MediAssist.Web.Models;

public enum PathwayStepState
{
    Done,
    Current,
    Attention,
    Upcoming
}

public sealed record PathwayStep(string Label, PathwayStepState State, bool IsCurrent);

/// <summary>
/// Display mapping from an application's status to its position on the Care Pathway:
/// Draft → Submitted → In review → Ready → Completed. "Needs correction" is shown as an
/// attention state on the review stage; "Cancelled" stops the pathway. Presentation only:
/// it does not decide or change any status.
/// </summary>
public static class ApplicationPathway
{
    public static IReadOnlyList<string> StageLabels { get; } =
        ["Draft", "Submitted", "In review", "Ready", "Completed"];

    public static int StageCount => StageLabels.Count;

    /// <summary>Zero-based stage index, or -1 when the application is cancelled.</summary>
    public static int StageIndex(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => 0,
        ApplicationStatus.Submitted => 1,
        ApplicationStatus.UnderReview => 2,
        ApplicationStatus.NeedsCorrection => 2,
        ApplicationStatus.ReadyForProcessing => 3,
        ApplicationStatus.Completed => 4,
        _ => -1
    };

    public static IReadOnlyList<PathwayStep> Steps(ApplicationStatus status)
    {
        var current = StageIndex(status);
        var steps = new List<PathwayStep>(StageCount);

        for (var i = 0; i < StageCount; i++)
        {
            var label = StageLabels[i];

            if (current < 0)
            {
                steps.Add(new(label, PathwayStepState.Upcoming, false));
            }
            else if (i < current)
            {
                steps.Add(new(label, PathwayStepState.Done, false));
            }
            else if (i == current)
            {
                if (status == ApplicationStatus.NeedsCorrection)
                    steps.Add(new("Needs correction", PathwayStepState.Attention, true));
                else if (status == ApplicationStatus.Completed)
                    steps.Add(new(label, PathwayStepState.Done, true));
                else
                    steps.Add(new(label, PathwayStepState.Current, true));
            }
            else
            {
                steps.Add(new(label, PathwayStepState.Upcoming, false));
            }
        }

        return steps;
    }

    /// <summary>"Stage 3 of 5: Needs correction", or "Cancelled".</summary>
    public static string Summary(ApplicationStatus status)
    {
        var index = StageIndex(status);
        return index < 0
            ? ApplicationStatusDisplay.Label(status)
            : $"Stage {index + 1} of {StageCount}: {ApplicationStatusDisplay.Label(status)}";
    }
}
