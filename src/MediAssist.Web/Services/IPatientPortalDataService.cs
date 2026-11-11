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
    Task<IReadOnlyList<ApplicationChecklist>> GetDocumentChecklistsAsync(CancellationToken cancellationToken = default);
}
