using Microsoft.Playwright;

namespace MediAssist.Web.E2E;

/// <summary>
/// The responsive and accessibility page checks run by hand in every phase since Milestone 2,
/// committed as tests (M3.3). Every route at 320, 768, 1024 and 1440 px, in a fresh browser (no
/// saved data; the runner portal shows its default runner), against a fresh shared store:
/// no sideways scrolling, 44 px targets, 13 px minimum text, one h1, at most one filled button,
/// unique ids, working ARIA references, no skipped heading levels, and AA contrast.
/// </summary>
[Collection("app")]
public sealed class PageChecksTests(AppFixture app)
{
    public static readonly string[] Routes =
    [
        "/", "/patient", "/patient/applications", "/patient/applications/MA-2026-00431", "/patient/applications/MA-2026-00418",
        "/patient/applications/MA-2026-00395", "/patient/applications/new", "/patient/applications/new?step=2", "/patient/documents",
        "/patient/hospitals", "/patient/hospitals/san-isidro-general", "/patient/notifications", "/patient/notifications?show=unread",
        "/patient/patients", "/patient/patients/new", "/patient/patients/maria", "/patient/patients/maria/edit", "/patient/patients/lia",
        "/patient/patients/lia/authorization", "/patient/programs", "/patient/runner", "/patient/runner/new", "/patient/settings",
        "/admin", "/admin/reviews", "/admin/reviews?stage=review", "/admin/reviews/MA-2026-00452", "/admin/reviews/MA-2026-00431",
        "/admin/reviews/MA-2026-00418", "/admin/reviews/MA-2026-00395", "/admin/reviews/MA-2026-99999", "/admin/authorizations",
        "/admin/authorizations?show=decided", "/admin/runners", "/admin/runners?show=assigned", "/admin/runners?show=closed",
        "/admin/programs", "/admin/users", "/admin/notifications", "/admin/settings", "/runner", "/runner/tasks",
        "/runner/tasks?show=accept", "/runner/tasks?show=done", "/runner/tasks/rr-33", "/runner/tasks/rr-24", "/runner/tasks/rr-29",
        "/runner/history", "/runner/notifications", "/runner/settings", "/not-found",
    ];

    [Theory]
    [InlineData(320)]
    [InlineData(768)]
    [InlineData(1024)]
    [InlineData(1440)]
    public async Task Every_page_passes_the_checks(int width)
    {
        await using var context = await app.Browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = 900 } });
        var page = await context.NewPageAsync();
        var failures = new List<string>();

        foreach (var route in Routes)
        {
            await page.GotoAsync(app.BaseUrl + route, new() { WaitUntil = WaitUntilState.Load });
            // Interactive content (the user's saved data, live counts) arrives after the circuit connects.
            await page.WaitForSelectorAsync("h1", new() { State = WaitForSelectorState.Attached });
            await page.WaitForTimeoutAsync(1500);
            var issues = await page.EvaluateAsync<string[]>(CheckScript, width);
            if (issues.Length > 0) failures.Add($"{width}px {route}: {string.Join("; ", issues)}");
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The same checks as the review-session suite, run in the page. Returns the problems found.</summary>
    private const string CheckScript = """
        (width) => {
          const doc = document, w = window, issues = [];
          const lum = c => { const m = c.match(/[\d.]+/g); if (!m) return null;
            const [r, g, b] = m.slice(0, 3).map(v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); });
            return { L: 0.2126 * r + 0.7152 * g + 0.0722 * b, a: m[3] === undefined ? 1 : +m[3] }; };
          const bg = el => { while (el) { const c = w.getComputedStyle(el); const l = lum(c.backgroundColor);
            if (c.backgroundImage !== 'none') return null; if (l && l.a > 0.9) return l.L; el = el.parentElement; } return 1; };

          if (doc.documentElement.scrollWidth > width + 1) issues.push('overflow ' + doc.documentElement.scrollWidth);
          const h1 = doc.querySelectorAll('h1'); if (h1.length !== 1) issues.push('h1 x' + h1.length);
          const filled = [...doc.querySelectorAll('main .btn--primary')].filter(e => e.offsetParent);
          if (filled.length > 1) issues.push('filled buttons x' + filled.length);

          const ids = {}; doc.querySelectorAll('[id]').forEach(e => ids[e.id] = (ids[e.id] || 0) + 1);
          Object.entries(ids).filter(([, v]) => v > 1).forEach(([k]) => issues.push('duplicate id ' + k));
          doc.querySelectorAll('[aria-labelledby],[aria-describedby],[aria-controls]').forEach(e =>
            ['aria-labelledby', 'aria-describedby', 'aria-controls'].forEach(a => { const v = e.getAttribute(a);
              if (v) v.split(/\s+/).forEach(id => { if (id && !doc.getElementById(id)) issues.push(a + ' -> missing ' + id); }); }));

          let prev = 0;
          doc.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach(h => {
            if (!h.offsetParent && w.getComputedStyle(h).position !== 'fixed') return;
            const n = +h.tagName[1]; if (prev && n > prev + 1) issues.push('heading skip h' + prev + '->h' + n); prev = n; });

          doc.querySelectorAll('a[href],button,input:not([type=hidden]),select,textarea,[role=button]').forEach(e => {
            if (!e.offsetParent || e.closest('.visually-hidden,.skip-link')) return;
            if (e.tagName === 'A' && w.getComputedStyle(e, '::after').position === 'absolute') return; // whole-row link
            let t = e; if (e.matches('input[type=radio],input[type=checkbox]')) t = e.closest('label') || e;
            const r = t.getBoundingClientRect(); if (r.width === 0) return;
            if (e.matches('main p a, main li > a:not([class]), .field__hint a, footer a')) return; // links inside text
            if (r.height < 43.5 || r.width < 43.5) issues.push('small target ' + Math.round(r.width) + 'x' + Math.round(r.height) + ' ' + (e.textContent || '').trim().slice(0, 25)); });

          const walker = doc.createTreeWalker(doc.body, NodeFilter.SHOW_TEXT); const seen = new Set(); let n;
          while ((n = walker.nextNode())) {
            if (!n.textContent.trim()) continue; const el = n.parentElement; if (!el || seen.has(el)) continue; seen.add(el);
            if (!el.offsetParent && w.getComputedStyle(el).position !== 'fixed') continue;
            if (el.closest('.visually-hidden,svg,#blazor-error-ui')) continue;
            const cs = w.getComputedStyle(el); const fs = parseFloat(cs.fontSize);
            if (fs < 12.95) issues.push('small text ' + fs + 'px ' + n.textContent.trim().slice(0, 20));
            const fg = lum(cs.color); const b = bg(el);
            if (fg && b !== null && fg.a > 0.9 && !el.closest('[disabled],:disabled')) {
              const ratio = (Math.max(fg.L, b) + 0.05) / (Math.min(fg.L, b) + 0.05);
              const large = fs >= 24 || (fs >= 18.66 && +cs.fontWeight >= 700);
              if (ratio < (large ? 3 : 4.5) - 0.01) issues.push('contrast ' + ratio.toFixed(2) + ' ' + n.textContent.trim().slice(0, 20)); } }
          return [...new Set(issues)];
        }
        """;
}
