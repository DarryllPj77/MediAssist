using MediAssist.Web.Data;
using MediAssist.Web.Models;
using MediAssist.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediAssist.Web.Tests;

// Shared test helpers (M3.3): a fixed clock in Philippine time, a throwaway SQLite file that can be
// made to fail, an in-memory "browser storage", and a harness that wires them to the real services.

/// <summary>A clock fixed at <see cref="Now"/> (settable), in Philippine time (UTC+8).</summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset Default = DateTimeOffset.Parse("2026-11-17T09:00:00+08:00");

    public TestClock() : this(Default) { }

    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone { get; } =
        TimeZoneInfo.CreateCustomTimeZone("PH", TimeSpan.FromHours(8), "Philippines", "Philippines");
}

/// <summary>A throwaway SQLite file for one test. Set <see cref="Fail"/> to make every write fail.</summary>
public sealed class TestDatabase : IDbContextFactory<WorkflowDbContext>, IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mediassist-test-{Guid.NewGuid():N}.db");

    public bool Fail { get; set; }

    public WorkflowDbContext CreateDbContext() => Fail
        ? throw new InvalidOperationException("Simulated database failure.")
        : new(new DbContextOptionsBuilder<WorkflowDbContext>().UseSqlite($"Data Source={Path};Pooling=False").Options);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(Path)) File.Delete(Path);
    }
}

/// <summary>One browser's storage, kept in memory. Set <see cref="FailSaves"/> to refuse every save.</summary>
public sealed class MemoryBrowserStorage : IWorkspaceStorage
{
    public WorkspaceSnapshot? Saved { get; set; }

    public bool FailSaves { get; set; }

    public Task<WorkspaceSnapshot?> LoadAsync() => Task.FromResult(Saved?.Upgrade());

    public Task<bool> SaveAsync(WorkspaceSnapshot snapshot)
    {
        if (FailSaves) return Task.FromResult(false);
        Saved = snapshot;
        return Task.FromResult(true);
    }
}

/// <summary>Fictional signed-in accounts for tests (M3.4b).</summary>
public static class TestActors
{
    public static Actor Patient { get; } = new("user-maria", "Maria Santos", Data.Roles.Patient);
    public static Actor Admin { get; } = new("user-ana", "Ana Reyes", Data.Roles.Administrator);
    /// <summary>A second patient account with no seeded data (M3.5).</summary>
    public static Actor OtherPatient { get; } = new("user-jun", "Jun Dela Cruz", Data.Roles.Patient);

    public static Actor Runner(string name) => new($"user-{name.Split(' ')[0].ToLowerInvariant()}", name, Data.Roles.Runner);
}

/// <summary>A fixed "who is signed in" for workspaces under test.</summary>
public sealed class FixedActor(Actor actor) : ICurrentActor
{
    public Task<Actor> GetAsync() => Task.FromResult(actor);
}

/// <summary>The seeded account's details, without Identity.</summary>
public sealed class StubAccountService(MockPortalStore store) : IAccountService
{
    public Task<PortalUser> GetCurrentAsync() => Task.FromResult(store.CurrentUser);
    public Task<AccountResult> SaveNameAsync(string? firstName, string? lastName) => throw new NotSupportedException();
}

/// <summary>The real services over a test database and clock: one shared store, any number of browsers.</summary>
public sealed class TestHarness : IDisposable
{
    private TestHarness(TestClock clock)
    {
        Clock = clock;
        Store = new MockPortalStore(clock) { SeedOwnerId = TestActors.Patient.UserId };
    }

    public TestClock Clock { get; }
    public TestDatabase Database { get; } = new();
    public MockPortalStore Store { get; }
    public WorkflowService Workflow { get; private set; } = default!;

    public static async Task<TestHarness> StartAsync(DateTimeOffset? now = null)
    {
        var harness = new TestHarness(new TestClock(now ?? TestClock.Default));
        await harness.RestartAsync();
        return harness;
    }

    /// <summary>A fresh shared store over the same database, as after a server restart.</summary>
    public async Task<WorkflowService> RestartAsync()
    {
        Workflow = new WorkflowService(Database, Store, Clock, NullLogger<WorkflowService>.Instance);
        await Workflow.InitializeAsync();
        return Workflow;
    }

    /// <summary>A loaded patient workspace (its own storage unless one is given; Maria's account unless another is given).</summary>
    public async Task<ApplicationWorkspace> BrowserAsync(MemoryBrowserStorage? storage = null, Actor? account = null)
    {
        var workspace = new ApplicationWorkspace(storage ?? new MemoryBrowserStorage(), Store, Workflow, Clock, new FixedActor(account ?? TestActors.Patient));
        await workspace.EnsureLoadedAsync();
        return workspace;
    }

    public void Dispose() => Database.Dispose();
}
