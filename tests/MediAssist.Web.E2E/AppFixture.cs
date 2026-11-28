using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace MediAssist.Web.E2E;

/// <summary>
/// Starts the built MediAssist app on a free local port with throwaway databases (so your own
/// App_Data is never touched), and one headless browser for the tests (M3.3). Since M3.4 the app
/// seeds its fictional demo accounts in Development; <see cref="PageAsync"/> signs in as one.
/// <para>
/// The app is found at <c>MEDIASSIST_APP_DLL</c>, else next to these tests in an artifacts layout,
/// else under <c>src/MediAssist.Web/bin</c>. The browser is Chromium, or the channel in
/// <c>MEDIASSIST_E2E_CHANNEL</c> (default on Windows: the installed Microsoft Edge, so no download
/// is needed locally; CI installs Chromium).
/// </para>
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private Process? _app;
    private IPlaywright? _playwright;
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"mediassist-e2e-{Guid.NewGuid():N}.db");
    private readonly string _identityDatabase = Path.Combine(Path.GetTempPath(), $"mediassist-e2e-identity-{Guid.NewGuid():N}.db");

    public string BaseUrl { get; private set; } = "";
    public IBrowser Browser { get; private set; } = default!;

    /// <summary>The Development demo password the app seeds its fictional accounts with (from appsettings.Development.json).</summary>
    public string DemoPassword { get; private set; } = "";

    /// <summary>A page in a fresh browser context of the given width, signed in as <paramref name="email"/> (null: signed out).</summary>
    public async Task<IPage> PageAsync(string? email, int width = 1024)
    {
        var context = await Browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = 900 } });
        var page = await context.NewPageAsync();
        if (email is null) return page;

        await page.GotoAsync(BaseUrl + "/account/login");
        await page.FillAsync("#login-email", email);
        await page.FillAsync("#login-password", DemoPassword);
        await page.ClickAsync("form.login__form button[type=submit]");
        await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
        return page;
    }

    public async Task InitializeAsync()
    {
        var repo = RepoRoot();
        var settings = System.Text.Json.JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(repo, "src", "MediAssist.Web", "appsettings.Development.json")));
        DemoPassword = settings.RootElement.GetProperty("Seed").GetProperty("DemoPassword").GetString()!;
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.Combine(repo, "src", "MediAssist.Web"), // content root (static web assets, appsettings)
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in new[] { AppDll(repo), "--urls", BaseUrl, $"--Workflow:DatabasePath={_database}", $"--Identity:DatabasePath={_identityDatabase}", "--RateLimiting:SignInPerMinute=1000" })
            start.ArgumentList.Add(arg);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        _app = Process.Start(start) ?? throw new InvalidOperationException("Could not start the app.");
        _app.OutputDataReceived += (_, _) => { };
        _app.ErrorDataReceived += (_, _) => { };
        _app.BeginOutputReadLine();
        _app.BeginErrorReadLine();
        await WaitUntilUpAsync();

        _playwright = await Playwright.CreateAsync();
        var channel = Environment.GetEnvironmentVariable("MEDIASSIST_E2E_CHANNEL") ?? (OperatingSystem.IsWindows() ? "msedge" : null);
        Browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true, Channel = string.IsNullOrEmpty(channel) ? null : channel });
    }

    private async Task WaitUntilUpAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var until = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < until)
        {
            if (_app!.HasExited) throw new InvalidOperationException($"The app stopped while starting (exit code {_app.ExitCode}).");
            try
            {
                if ((await http.GetAsync(BaseUrl)).StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(500);
        }
        throw new TimeoutException($"The app did not answer at {BaseUrl} within 90 seconds.");
    }

    private static string AppDll(string repo)
    {
        if (Environment.GetEnvironmentVariable("MEDIASSIST_APP_DLL") is { Length: > 0 } configured) return configured;

        // Artifacts layout: .../bin/MediAssist.Web.E2E/<config>/ next to .../bin/MediAssist.Web/<config>/
        var here = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var sibling = Path.Combine(here.Parent!.Parent!.FullName, "MediAssist.Web", here.Name, "MediAssist.Web.dll");
        if (File.Exists(sibling)) return sibling;

        // Default layout: tests/MediAssist.Web.E2E/bin/<Configuration>/net10.0/
        var configuration = here.Parent!.Name;
        var standard = Path.Combine(repo, "src", "MediAssist.Web", "bin", configuration, here.Name, "MediAssist.Web.dll");
        if (File.Exists(standard)) return standard;

        throw new FileNotFoundException("Build the app first, or set MEDIASSIST_APP_DLL.", standard);
    }

    private static string RepoRoot([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", ".."));

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        _playwright?.Dispose();
        if (_app is { HasExited: false })
        {
            _app.Kill(entireProcessTree: true);
            await _app.WaitForExitAsync();
        }
        _app?.Dispose();
        foreach (var file in new[] { _database, _database + "-shm", _database + "-wal", _identityDatabase, _identityDatabase + "-shm", _identityDatabase + "-wal" })
            if (File.Exists(file)) File.Delete(file);
    }
}

[CollectionDefinition("app")]
public sealed class AppCollection : ICollectionFixture<AppFixture>;
