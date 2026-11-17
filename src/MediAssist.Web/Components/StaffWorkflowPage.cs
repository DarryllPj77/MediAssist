using MediAssist.Web.Services;
using Microsoft.AspNetCore.Components;

namespace MediAssist.Web.Components;

/// <summary>
/// A staff page that shows shared workflow data (D9a): it loads in <see cref="LoadAsync"/> and
/// loads again whenever anyone (a patient upload, another reviewer) changes the shared store.
/// </summary>
public abstract class StaffWorkflowPage : ComponentBase, IDisposable
{
    [Inject] protected WorkflowService Workflow { get; set; } = default!;

    /// <summary>(Re)read everything the page shows.</summary>
    protected abstract Task LoadAsync();

    protected override void OnInitialized() => Workflow.Changed += OnWorkflowChanged;

    protected override Task OnParametersSetAsync() => LoadAsync();

    private void OnWorkflowChanged() => _ = InvokeAsync(async () =>
    {
        await LoadAsync();
        StateHasChanged();
    });

    public virtual void Dispose()
    {
        Workflow.Changed -= OnWorkflowChanged;
        GC.SuppressFinalize(this);
    }
}
