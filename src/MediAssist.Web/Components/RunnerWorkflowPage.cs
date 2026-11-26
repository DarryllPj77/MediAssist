using MediAssist.Web.Services;
using Microsoft.AspNetCore.Components;

namespace MediAssist.Web.Components;

/// <summary>
/// A runner portal page (D10): loads again when the shared store changes (an administrator assigns,
/// a family cancels). The runner is the signed-in Runner account (M3.4).
/// </summary>
public abstract class RunnerWorkflowPage : StaffWorkflowPage
{
    [Inject] protected RunnerSession Runner { get; set; } = default!;

    /// <summary>The signed-in runner's name ("" for an account that isn't a known runner: no errands match).</summary>
    protected string CurrentRunner => Runner.Current?.Name ?? "";

    protected override async Task OnParametersSetAsync()
    {
        await Runner.EnsureLoadedAsync();
        await base.OnParametersSetAsync();
    }
}
