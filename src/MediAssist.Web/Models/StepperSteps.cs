namespace MediAssist.Web.Models;

public enum StepState
{
    Done,
    Current,
    Attention,
    Upcoming
}

/// <summary>One stage in a chevron stepper: English label, Filipino helper label, state.</summary>
public sealed record StepperStep(string Label, string LabelFilipino, StepState State);

/// <summary>
/// The single source of journey stages for both progress visuals (ChevronStepper and StepMeter),
/// plus the wizard and runner journeys. Display only: they read a status and describe it;
/// they never decide or change one. Stage names follow the Phase 1 status vocabulary.
/// </summary>
public static class StepperSteps
{
    private static readonly (string En, string Fil)[] ApplicationStages =
    [
        ("Draft", "Borador"),
        ("Submitted", "Naisumite"),
        ("Under review", "Sinusuri"),
        ("Ready for processing", "Handa nang iproseso"),
        ("Completed", "Tapos na"),
    ];

    private static readonly (string En, string Fil)[] WizardStages =
    [
        ("Select patient", "Piliin ang pasyente"),
        ("Select hospital", "Piliin ang ospital"),
        ("Choose program", "Piliin ang programa"),
        ("Prepare documents", "Ihanda ang papeles"),
        ("Review and submit", "Suriin at isumite"),
    ];

    private static readonly (string En, string Fil)[] RunnerStages =
    [
        ("Assigned", "Naitalaga"),
        ("Accepted", "Tinanggap"),
        ("In progress", "Ginagawa"),
        ("Completed", "Natapos"),
    ];

    public static int ApplicationStageCount => ApplicationStages.Length;

    /// <summary>Zero-based stage of an application status, or -1 when cancelled.</summary>
    public static int ApplicationStageIndex(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => 0,
        ApplicationStatus.Submitted => 1,
        ApplicationStatus.UnderReview or ApplicationStatus.NeedsCorrection => 2,
        ApplicationStatus.ReadyForProcessing => 3,
        ApplicationStatus.Completed => 4,
        _ => -1
    };

    /// <summary>"Step 3 of 5", or "Stopped" for a cancelled application.</summary>
    public static string ApplicationStepText(ApplicationStatus status) =>
        ApplicationStageIndex(status) is var i and >= 0 ? $"Step {i + 1} of {ApplicationStageCount}" : "Stopped";

    public static IReadOnlyList<StepperStep> ForApplication(ApplicationStatus status)
    {
        var current = ApplicationStageIndex(status);

        return ApplicationStages.Select((s, i) =>
        {
            if (current < 0) return new StepperStep(s.En, s.Fil, StepState.Upcoming);
            if (i < current || (i == current && status == ApplicationStatus.Completed))
                return new StepperStep(s.En, s.Fil, StepState.Done);
            if (i > current) return new StepperStep(s.En, s.Fil, StepState.Upcoming);
            return status == ApplicationStatus.NeedsCorrection
                ? new StepperStep("Needs correction", "Kailangang ayusin", StepState.Attention)
                : new StepperStep(s.En, s.Fil, StepState.Current);
        }).ToList();
    }

    /// <summary>Wizard steps with <paramref name="current"/> as a zero-based index.</summary>
    public static IReadOnlyList<StepperStep> ForWizard(int current) =>
        WizardStages.Select((s, i) => new StepperStep(s.En, s.Fil,
            i < current ? StepState.Done : i == current ? StepState.Current : StepState.Upcoming)).ToList();

    public static IReadOnlyList<StepperStep> ForRunnerTask(RunnerTaskStatus status)
    {
        var current = status switch
        {
            RunnerTaskStatus.Assigned => 0,
            RunnerTaskStatus.Accepted => 1,
            RunnerTaskStatus.InProgress => 2,
            RunnerTaskStatus.Completed => 3,
            _ => -1
        };

        return RunnerStages.Select((s, i) => new StepperStep(s.En, s.Fil,
            current < 0 ? StepState.Upcoming
            : i < current || (i == current && status == RunnerTaskStatus.Completed) ? StepState.Done
            : i == current ? StepState.Current
            : StepState.Upcoming)).ToList();
    }
}
