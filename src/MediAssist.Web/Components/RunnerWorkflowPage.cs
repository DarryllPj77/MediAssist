using MediAssist.Web.Services;
using Microsoft.AspNetCore.Components;

namespace MediAssist.Web.Components;

/// <summary>
/// A runner portal page (D10): loads again when the shared store changes (an administrator assigns,
/// a family cancels) and when the runner chosen in settings changes.
/// </summary>
public abstract class RunnerWorkflowPage : StaffWorkflowPage
{
    [Inject] protected RunnerSession Runner { get; set; } = default!;

    protected string CurrentRunner => Runner.Current.Name;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Runner.Changed += OnRunnerChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Runner.EnsureLoadedAsync();
    }

    private void OnRunnerChanged() => _ = InvokeAsync(async () =>
    {
        await LoadAsync();
        StateHasChanged();
    });

    public override void Dispose()
    {
        Runner.Changed -= OnRunnerChanged;
        base.Dispose();
    }
}
