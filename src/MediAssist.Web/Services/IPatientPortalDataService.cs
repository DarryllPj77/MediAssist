using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// Read model for the Patient Portal. The prototype uses <see cref="MockPatientPortalDataService"/>;
/// an EF Core / PostgreSQL implementation will replace it without UI changes.
/// </summary>
public interface IPatientPortalDataService
{
    Task<PortalUser> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientProfile>> GetPatientProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistanceApplication>> GetApplicationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActionItem>> GetActionItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalNotification>> GetNotificationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistanceProgram>> GetProgramsAsync(CancellationToken cancellationToken = default);

    /// <summary>Hospitals an application can name (the wizard's hospital step).</summary>
    Task<IReadOnlyList<string>> GetHospitalsAsync(CancellationToken cancellationToken = default);
    /// <summary>Checklists of the household's applications that still need documents.</summary>
    Task<IReadOnlyList<ApplicationChecklist>> GetDocumentChecklistsAsync(CancellationToken cancellationToken = default);

    /// <summary>One of the household's applications with its program, checklist, history and runner errands; null if not found.</summary>
    Task<ApplicationDetail?> GetApplicationAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>An application's activity history, oldest first (empty if not the household's).</summary>
    Task<IReadOnlyList<ApplicationEvent>> GetApplicationHistoryAsync(string reference, CancellationToken cancellationToken = default);
}
