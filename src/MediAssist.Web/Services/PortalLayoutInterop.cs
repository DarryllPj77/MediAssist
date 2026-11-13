using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MediAssist.Web.Services;

/// <summary>
/// Thin wrapper over wwwroot/js/portalLayout.js. Only call from OnAfterRenderAsync or event
/// handlers: JS interop is unavailable during prerendering.
/// </summary>
public sealed class PortalLayoutInterop(IJSRuntime js) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _module;

    private Task<IJSObjectReference> Module =>
        _module ??= js.InvokeAsync<IJSObjectReference>("import", "./js/portalLayout.js").AsTask();

    public sealed record ViewportWatch(int Id, bool IsWide);

    public async ValueTask<string?> GetSidebarPreferenceAsync() =>
        await (await Module).InvokeAsync<string?>("getSidebarPreference");

    public async ValueTask<bool> SetSidebarPreferenceAsync(string value) =>
        await (await Module).InvokeAsync<bool>("setSidebarPreference", value);

    public async ValueTask<string?> GetFilipinoPreferenceAsync() =>
        await (await Module).InvokeAsync<string?>("getFilipinoPreference");

    public async ValueTask<bool> SetFilipinoPreferenceAsync(string value) =>
        await (await Module).InvokeAsync<bool>("setFilipinoPreference", value);

    public async ValueTask<ViewportWatch> WatchWideViewportAsync<T>(DotNetObjectReference<T> target) where T : class =>
        await (await Module).InvokeAsync<ViewportWatch>("watchWideViewport", target);

    public async ValueTask UnwatchWideViewportAsync(int id) =>
        await (await Module).InvokeVoidAsync("unwatchWideViewport", id);

    public async ValueTask EnableTransitionsAfterPaintAsync(ElementReference element) =>
        await (await Module).InvokeVoidAsync("enableTransitionsAfterPaint", element);

    public async ValueTask InitDrawerAsync<T>(ElementReference dialog, DotNetObjectReference<T> target) where T : class =>
        await (await Module).InvokeVoidAsync("initDrawer", dialog, target);

    public async ValueTask OpenDrawerAsync(ElementReference dialog) =>
        await (await Module).InvokeVoidAsync("openDrawer", dialog);

    public async ValueTask CloseDrawerAsync(ElementReference dialog) =>
        await (await Module).InvokeVoidAsync("closeDrawer", dialog);

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try
        {
            var module = await _module;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone; nothing to clean up on the client.
        }
    }
}
