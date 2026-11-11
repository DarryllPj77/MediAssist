using Microsoft.AspNetCore.Components.Routing;

namespace MediAssist.Web.Navigation;

/// <summary>The three MediAssist portals (Phase 1 roles). Prototype: chosen by URL, no authentication.</summary>
public enum PortalRole
{
    Patient,
    Administrator,
    Runner
}

/// <summary>
/// Everything the shared shell needs to render one portal: its label, destinations
/// for the sidebar, bottom bar and drawer, and its optional primary action.
/// </summary>
public sealed record NavSet(
    PortalRole Role,
    string PortalLabel,
    string PortalLabelFilipino,
    NavDestination Home,
    IReadOnlyList<NavDestination> All,
    IReadOnlyList<NavDestination> BottomBar,
    IReadOnlyList<NavDestination> Secondary,
    NavDestination Notifications,
    NavDestination Settings,
    string? PrimaryActionHref = null,
    string? PrimaryActionLabel = null,
    string? PrimaryActionShortLabel = null)
{
    public bool IsActive(NavDestination destination, string relativePath)
    {
        var path = RoleNavigation.Normalize(relativePath);

        if (destination.Match == NavLinkMatch.All)
            return string.Equals(path, destination.Href, StringComparison.OrdinalIgnoreCase)
                || (Role == PortalRole.Patient && destination == Home && path == "/");

        // A section never claims the primary action's own page (e.g. /applications vs /applications/new).
        var actionPath = PrimaryActionHref?.Split('?')[0];
        if (actionPath is not null &&
            !string.Equals(actionPath, destination.Href, StringComparison.OrdinalIgnoreCase) &&
            path.StartsWith(actionPath, StringComparison.OrdinalIgnoreCase))
            return false;

        return string.Equals(path, destination.Href, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(destination.Href + "/", StringComparison.OrdinalIgnoreCase);
    }

    public NavDestination? Resolve(string relativePath) =>
        All.Where(d => IsActive(d, relativePath))
           .OrderByDescending(d => d.Href.Length)
           .FirstOrDefault();
}

public static class RoleNavigation
{
    // ---- Patient (existing destinations, unchanged) ----
    public static NavSet Patient { get; } = new(
        PortalRole.Patient,
        "Patient portal", "Para sa pasyente at pamilya",
        PortalNavigation.Dashboard,
        PortalNavigation.All,
        PortalNavigation.BottomBar,
        PortalNavigation.Secondary,
        PortalNavigation.Notifications,
        PortalNavigation.Settings,
        PortalNavigation.NewApplicationHref, "New application", "New");

    // ---- Administrator ----
    public static readonly NavDestination AdminHome =
        new("Dashboard", "/admin", "home", NavGroup.Overview, NavLinkMatch.All, ShortLabel: "Home");
    public static readonly NavDestination AdminReviews =
        new("Review queue", "/admin/reviews", "clipboard-check", NavGroup.Overview, ShortLabel: "Reviews");
    public static readonly NavDestination AdminAuthorizations =
        new("Authorization reviews", "/admin/authorizations", "shield-check", NavGroup.Overview, ShortLabel: "Authorizations");
    public static readonly NavDestination AdminPrograms =
        new("Hospitals & programs", "/admin/programs", "heart", NavGroup.Manage);
    public static readonly NavDestination AdminRunners =
        new("Runner management", "/admin/runners", "route", NavGroup.Manage, ShortLabel: "Runners");
    public static readonly NavDestination AdminUsers =
        new("Users", "/admin/users", "users", NavGroup.Manage);
    public static readonly NavDestination AdminNotifications =
        new("Notifications", "/admin/notifications", "bell", NavGroup.Account, ShortLabel: "Alerts");
    public static readonly NavDestination AdminSettings =
        new("Profile & Settings", "/admin/settings", "user-circle", NavGroup.Account);

    public static NavSet Administrator { get; } = new(
        PortalRole.Administrator,
        "Administrator", "Tagapangasiwa",
        AdminHome,
        [AdminHome, AdminReviews, AdminAuthorizations, AdminPrograms, AdminRunners, AdminUsers, AdminNotifications, AdminSettings],
        [AdminHome, AdminReviews, AdminRunners],
        [AdminAuthorizations, AdminPrograms, AdminUsers, AdminNotifications, AdminSettings],
        AdminNotifications,
        AdminSettings,
        "/admin/reviews?next=1", "Review next application", "Next");

    // ---- Medical runner ----
    public static readonly NavDestination RunnerHome =
        new("Today", "/runner", "home", NavGroup.Tasks, NavLinkMatch.All);
    public static readonly NavDestination RunnerTasks =
        new("My assignments", "/runner/tasks", "list-checks", NavGroup.Tasks, ShortLabel: "Assignments");
    public static readonly NavDestination RunnerHistory =
        new("Task history", "/runner/history", "clock", NavGroup.Tasks, ShortLabel: "History");
    public static readonly NavDestination RunnerNotifications =
        new("Notifications", "/runner/notifications", "bell", NavGroup.Account, ShortLabel: "Alerts");
    public static readonly NavDestination RunnerSettings =
        new("Profile & Settings", "/runner/settings", "user-circle", NavGroup.Account);

    public static NavSet Runner { get; } = new(
        PortalRole.Runner,
        "Runner", "Para sa runner",
        RunnerHome,
        [RunnerHome, RunnerTasks, RunnerHistory, RunnerNotifications, RunnerSettings],
        [RunnerHome, RunnerTasks, RunnerHistory, RunnerNotifications],
        [RunnerSettings],
        RunnerNotifications,
        RunnerSettings);

    /// <summary>Prototype role resolution: the URL decides the portal (/admin, /runner, else patient).</summary>
    public static NavSet ForPath(string relativePath)
    {
        var path = Normalize(relativePath);
        if (path == "/admin" || path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase)) return Administrator;
        if (path == "/runner" || path.StartsWith("/runner/", StringComparison.OrdinalIgnoreCase)) return Runner;
        return Patient;
    }

    internal static string Normalize(string relativePath) =>
        "/" + relativePath.Split('?', '#')[0].Trim('/');
}
