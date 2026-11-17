using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// Which fictional runner the runner portal shows (D10). The prototype has no sign-in, so the
/// runner is chosen in Runner settings and kept in this browser (plain localStorage; nothing
/// personal). This is a demonstration convenience, not authentication. Defaults to the first
/// runner until the saved choice is read after the page is interactive.
/// </summary>
public sealed class RunnerSession(PortalLayoutInterop interop)
{
    private Task? _loading;

    public event Action? Changed;

    public bool IsLoaded { get; private set; }

    public RunnerProfile Current { get; private set; } = StaffDecisionRules.Runners[0];

    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        var saved = await interop.GetRunnerPreferenceAsync();
        Current = StaffDecisionRules.Runners.FirstOrDefault(r => r.Name == saved) ?? StaffDecisionRules.Runners[0];
        IsLoaded = true;
        Changed?.Invoke();
    }

    /// <summary>Switch runner now; returns false when the browser would not keep the choice for next time.</summary>
    public async Task<bool> SetAsync(string name)
    {
        if (StaffDecisionRules.Runners.FirstOrDefault(r => r.Name == name) is not { } runner) return false;
        Current = runner;
        Changed?.Invoke();
        return await interop.SetRunnerPreferenceAsync(runner.Name);
    }
}
