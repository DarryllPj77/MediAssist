// MediAssist portal layout helpers (ES module, loaded on demand via JS interop).
// Kept intentionally small: persistence, one media-query watcher, and native <dialog> control.

const SIDEBAR_KEY = "mediassist.sidebar";
const WIDE_QUERY = "(min-width: 75em)";
const TABLET_QUERY = "(min-width: 48em)";
const watchers = new Map();
let nextWatcherId = 1;

export function getSidebarPreference() {
    try { return localStorage.getItem(SIDEBAR_KEY); } catch { return null; }
}

export function setSidebarPreference(value) {
    try { localStorage.setItem(SIDEBAR_KEY, value); } catch { /* storage unavailable: ignore */ }
}

export function watchWideViewport(dotNetRef) {
    const mq = window.matchMedia(WIDE_QUERY);
    const handler = (e) => dotNetRef.invokeMethodAsync("OnWideViewportChanged", e.matches);
    mq.addEventListener("change", handler);
    const id = nextWatcherId++;
    watchers.set(id, () => mq.removeEventListener("change", handler));
    return { id, isWide: mq.matches };
}

export function unwatchWideViewport(id) {
    watchers.get(id)?.();
    watchers.delete(id);
}

// Enable layout transitions only after the restored sidebar state has been painted,
// so restoring a saved preference on load never animates.
export function enableTransitionsAfterPaint(element) {
    requestAnimationFrame(() => requestAnimationFrame(() => {
        if (element) element.dataset.ready = "true";
    }));
}

export function initDrawer(dialog, dotNetRef) {
    if (!dialog || dialog.dataset.initialized) return;
    dialog.dataset.initialized = "true";
    dialog.addEventListener("close", () => dotNetRef.invokeMethodAsync("OnDrawerClosed"));
    // Clicking the backdrop (the dialog element itself, outside the panel) closes it.
    dialog.addEventListener("click", (e) => { if (e.target === dialog) dialog.close(); });
    // The drawer is mobile-only: close it if the viewport grows past the tablet breakpoint.
    window.matchMedia(TABLET_QUERY).addEventListener("change", (e) => {
        if (e.matches && dialog.open) dialog.close();
    });
}

// Called from PortalLayoutInterop.OpenDrawerAsync / CloseDrawerAsync (MobileNavDrawer).
export function openDrawer(dialog) {
    if (dialog && !dialog.open) dialog.showModal();
}

export function closeDrawer(dialog) {
    if (dialog && dialog.open) dialog.close();
}
