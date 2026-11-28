using Microsoft.EntityFrameworkCore;

namespace MediAssist.Web.Data;

/// <summary>
/// The shared workflow store (D9a): SQLite through EF Core. Each row keeps its record as JSON with
/// the fields staff filter on alongside, which suits a prototype whose records still change shape.
/// Since M3.6 the schema is managed by EF Core migrations (<c>Data/Migrations/Workflow</c>); databases
/// made earlier with <c>EnsureCreated</c> are recognised and brought under migrations on start-up.
/// <c>Version</c> columns are concurrency tokens: a write based on an out-of-date row is refused.
/// </summary>
public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<StoredApplication> Applications => Set<StoredApplication>();
    public DbSet<StoredUpload> Uploads => Set<StoredUpload>();
    public DbSet<StoredNotification> Notifications => Set<StoredNotification>();
    public DbSet<StoredRecord> Authorizations => Set<StoredRecord>("Authorizations");
    public DbSet<StoredRecord> RunnerRequests => Set<StoredRecord>("RunnerRequests");

    /// <summary>
    /// Tables a D9a-era database (made by <c>EnsureCreated</c> before D9b) lacks; created before such a
    /// database is brought under migrations (M3.6). Not used for new databases.
    /// </summary>
    public const string AddLaterTables = """
        CREATE TABLE IF NOT EXISTS "Authorizations" ("Id" TEXT NOT NULL CONSTRAINT "PK_Authorizations" PRIMARY KEY, "Json" TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS "RunnerRequests" ("Id" TEXT NOT NULL CONSTRAINT "PK_RunnerRequests" PRIMARY KEY, "Json" TEXT NOT NULL);
        """;

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<StoredApplication>(e =>
        {
            e.HasKey(a => a.Reference);
            e.Property(a => a.Reference).HasMaxLength(32);
            e.Property(a => a.Version).IsConcurrencyToken();
            e.HasIndex(a => a.Status);
        });
        model.Entity<StoredUpload>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasMaxLength(32);
            e.HasIndex(u => u.Reference);
        });
        model.Entity<StoredNotification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasMaxLength(80);
        });
        // One record type, two tables (shared-type entities): authorization and runner requests.
        foreach (var table in new[] { "Authorizations", "RunnerRequests" })
            model.SharedTypeEntity<StoredRecord>(table, e =>
            {
                e.ToTable(table);
                e.HasKey(r => r.Id);
                e.Property(r => r.Version).IsConcurrencyToken();
            });
    }
}

/// <summary>A submitted (or reviewed) application: <see cref="Json"/> holds the whole <c>SharedApplication</c>.</summary>
public sealed class StoredApplication
{
    public string Reference { get; set; } = "";
    public Guid PatientId { get; set; }
    public int Status { get; set; }
    public int Version { get; set; }
    public string Json { get; set; } = "";
}

/// <summary>One uploaded version of a document: <see cref="Json"/> holds the <c>DocumentUpload</c>.</summary>
public sealed class StoredUpload
{
    public string Id { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Json { get; set; } = "";
}

/// <summary>A request (authorization or runner, D9b): <see cref="Json"/> holds the shared record.</summary>
public sealed class StoredRecord
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Json { get; set; } = "";
}

/// <summary>A notification for the account holder: <see cref="Json"/> holds the <c>PortalNotification</c>.</summary>
public sealed class StoredNotification
{
    public string Id { get; set; } = "";
    public string Json { get; set; } = "";
}
