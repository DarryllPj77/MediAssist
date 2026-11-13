using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// Display preferences for this browser (D8): the sidebar layout and whether Filipino helper text
/// shows. Saved in plain localStorage (they hold nothing personal) and shared by every role, so a
/// change in Settings applies to the layout at once. Load only after the page is interactive.
/// </summary>
public sealed class UiPreferences(PortalLayoutInterop interop)
{
    private Task? _loading;

    public event Action? Changed;

    public bool IsLoaded { get; private set; }
    public SidebarPreference Sidebar { get; private set; } = SidebarPreference.Auto;
    public bool ShowFilipino { get; private set; } = true;

    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        Sidebar = await interop.GetSidebarPreferenceAsync() switch
        {
            "expanded" => SidebarPreference.Expanded,
            "collapsed" => SidebarPreference.Collapsed,
            _ => SidebarPreference.Auto
        };
        ShowFilipino = await interop.GetFilipinoPreferenceAsync() != "hide";
        IsLoaded = true;
        Changed?.Invoke();
    }

    /// <summary>Apply the choice now; returns false when the browser would not keep it for next time.</summary>
    public async Task<bool> SetSidebarAsync(SidebarPreference value)
    {
        Sidebar = value;
        Changed?.Invoke();
        return await interop.SetSidebarPreferenceAsync(value.ToString().ToLowerInvariant());
    }

    /// <summary>Apply the choice now; returns false when the browser would not keep it for next time.</summary>
    public async Task<bool> SetShowFilipinoAsync(bool show)
    {
        ShowFilipino = show;
        Changed?.Invoke();
        return await interop.SetFilipinoPreferenceAsync(show ? "show" : "hide");
    }
}
