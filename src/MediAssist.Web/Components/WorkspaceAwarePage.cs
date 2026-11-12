using MediAssist.Web.Services;
using Microsoft.AspNetCore.Components;

namespace MediAssist.Web.Components;

/// <summary>
/// Base for patient pages that show applications. The user's drafts and submissions live in
/// their browser and arrive after the first render (the layout loads them); when the workspace
/// loads or changes, the page runs <see cref="LoadAsync"/> again and re-renders.
/// </summary>
public abstract class WorkspaceAwarePage : ComponentBase, IDisposable
{
    [Inject] protected ApplicationWorkspace Workspace { get; set; } = default!;

    /// <summary>(Re)read everything the page shows from the data service.</summary>
    protected abstract Task LoadAsync();

    protected override void OnInitialized() => Workspace.Changed += OnWorkspaceChanged;

    protected override Task OnParametersSetAsync() => LoadAsync();

    private void OnWorkspaceChanged() => _ = InvokeAsync(async () =>
    {
        await LoadAsync();
        StateHasChanged();
    });

    public virtual void Dispose()
    {
        Workspace.Changed -= OnWorkspaceChanged;
        GC.SuppressFinalize(this);
    }
}
