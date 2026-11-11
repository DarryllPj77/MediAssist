using Microsoft.AspNetCore.Components.Routing;

namespace MediAssist.Web.Navigation;

public enum NavGroup
{
    Overview,
    FindHelp,
    Account
}

public sealed record NavDestination(
    string Label,
    string Href,
    string Icon,
    NavGroup Group,
    NavLinkMatch Match = NavLinkMatch.Prefix,
    string? ShortLabel = null);

/// <summary>
/// Single source of truth for Patient Portal destinations, used by the desktop sidebar,
/// the mobile bottom navigation, the mobile drawer and the header title.
/// </summary>
public static class PortalNavigation
{
    public const string NewApplicationHref = "/patient/applications/new";

    public static readonly NavDestination Dashboard =
        new("Dashboard", "/patient", "home", NavGroup.Overview, NavLinkMatch.All, ShortLabel: "Home");

    public static readonly NavDestination Applications =
        new("My Applications", "/patient/applications", "file-text", NavGroup.Overview, ShortLabel: "Applications");

    public static readonly NavDestination Patients =
        new("Patient Profiles", "/patient/patients", "users", NavGroup.Overview, ShortLabel: "Patients");

    public static readonly NavDestination Hospitals =
        new("Hospitals", "/patient/hospitals", "hospital", NavGroup.FindHelp);

    public static readonly NavDestination Programs =
        new("Assistance Programs", "/patient/programs", "heart", NavGroup.FindHelp);

    public static readonly NavDestination Documents =
        new("Document Checklist", "/patient/documents", "clipboard-check", NavGroup.FindHelp);

    public static readonly NavDestination Runner =
        new("Runner Assistance", "/patient/runner", "route", NavGroup.FindHelp);

    public static readonly NavDestination Notifications =
        new("Notifications", "/patient/notifications", "bell", NavGroup.Account, ShortLabel: "Alerts");

    public static readonly NavDestination Settings =
        new("Profile & Settings", "/patient/settings", "user-circle", NavGroup.Account);

    public static IReadOnlyList<NavDestination> All { get; } =
    [
        Dashboard, Applications, Patients,
        Hospitals, Programs, Documents, Runner,
        Notifications, Settings
    ];

    /// <summary>Mobile bottom bar (the New Application action sits in the middle).</summary>
    public static IReadOnlyList<NavDestination> BottomBar { get; } = [Dashboard, Applications, Patients];

    /// <summary>Destinations reachable from the mobile drawer (everything not in the bottom bar).</summary>
    public static IReadOnlyList<NavDestination> Secondary { get; } =
        [Hospitals, Programs, Documents, Runner, Notifications, Settings];

    public static bool IsActive(NavDestination destination, string relativePath)
    {
        var path = Normalize(relativePath);
        if (destination.Match == NavLinkMatch.All)
            return string.Equals(path, destination.Href, StringComparison.OrdinalIgnoreCase)
                || (destination == Dashboard && path == "/");

        // Prefix match on whole segments, and never let /applications claim /applications/new.
        if (path.StartsWith(NewApplicationHref, StringComparison.OrdinalIgnoreCase)) return false;
        return string.Equals(path, destination.Href, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(destination.Href + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string relativePath) =>
        "/" + relativePath.Split('?', '#')[0].Trim('/');

    public static string GroupLabel(NavGroup group) => group switch
    {
        NavGroup.Overview => "Overview",
        NavGroup.FindHelp => "Find help",
        NavGroup.Account => "Account",
        _ => group.ToString()
    };

    /// <summary>Resolve the current destination for the header title (longest matching prefix).</summary>
    public static NavDestination? Resolve(string relativePath)
    {
        var path = Normalize(relativePath);
        if (path.StartsWith(NewApplicationHref, StringComparison.OrdinalIgnoreCase))
            return new("New Application", NewApplicationHref, "plus", NavGroup.Overview);

        return All
            .Where(d => IsActive(d, path))
            .OrderByDescending(d => d.Href.Length)
            .FirstOrDefault();
    }
}
