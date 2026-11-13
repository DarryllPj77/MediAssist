using MediAssist.Web.Models;

namespace MediAssist.Web.Navigation;

/// <summary>Links into the My applications page, so every filter link is built one way.</summary>
public static class ApplicationsHref
{
    /// <summary>The list filtered by stage and patient; All and no patient give the bare page.</summary>
    public static string For(ApplicationStageFilter stage, string? patientKey = null)
    {
        var query = new List<string>();
        if (stage != ApplicationStageFilter.All) query.Add($"stage={stage.Key}");
        if (!string.IsNullOrEmpty(patientKey)) query.Add($"patient={Uri.EscapeDataString(patientKey)}");
        return query.Count == 0
            ? PortalNavigation.Applications.Href
            : $"{PortalNavigation.Applications.Href}?{string.Join('&', query)}";
    }

    /// <summary>One application's detail page.</summary>
    public static string Detail(string reference) => $"{PortalNavigation.Applications.Href}/{reference}";

    /// <summary>
    /// The URL key for a patient: their first name, lower case ("jose"). The earliest household
    /// member with that first name keeps the plain key, so existing links never change; a later one
    /// (seeded patients come first, then added ones by date) gets a short id suffix ("jose-3f2a").
    /// </summary>
    public static string PatientKey(PatientProfile patient, IReadOnlyList<PatientProfile>? household = null)
    {
        var first = FirstName(patient);
        var earlier = household?.TakeWhile(p => p.Id != patient.Id).Any(p => FirstName(p) == first) ?? false;
        return earlier ? $"{first}-{patient.Id.ToString("N")[..4]}" : first;
    }

    /// <summary>The household patient a key (or a patient id, as the program search sends) names.</summary>
    public static PatientProfile? FindPatient(IReadOnlyList<PatientProfile> household, string? key) =>
        string.IsNullOrWhiteSpace(key) ? null
        : household.FirstOrDefault(p => string.Equals(PatientKey(p, household), key.Trim(), StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(p.Id.ToString(), key.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>A patient's profile page.</summary>
    public static string Patient(PatientProfile patient, IReadOnlyList<PatientProfile> household) =>
        $"{PortalNavigation.Patients.Href}/{PatientKey(patient, household)}";

    private static string FirstName(PatientProfile patient) =>
        (patient.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "patient").ToLowerInvariant();
}
