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

    /// <summary>The URL key for a patient: their first name, lower case ("jose").</summary>
    public static string PatientKey(PatientProfile patient) =>
        patient.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
}
