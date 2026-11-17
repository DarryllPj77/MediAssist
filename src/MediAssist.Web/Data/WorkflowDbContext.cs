using Microsoft.EntityFrameworkCore;

namespace MediAssist.Web.Data;

/// <summary>
/// The shared workflow store (D9a): SQLite through EF Core. Each row keeps its record as JSON with
/// the fields staff filter on alongside, which suits a prototype whose records still change shape.
/// The schema is created on first run (<c>EnsureCreated</c>); there are no migrations yet.
/// </summary>
public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<StoredApplication> Applications => Set<StoredApplication>();
    public DbSet<StoredUpload> Uploads => Set<StoredUpload>();
    public DbSet<StoredNotification> Notifications => Set<StoredNotification>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<StoredApplication>(e =>
        {
            e.HasKey(a => a.Reference);
            e.Property(a => a.Reference).HasMaxLength(32);
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

/// <summary>A notification for the account holder: <see cref="Json"/> holds the <c>PortalNotification</c>.</summary>
public sealed class StoredNotification
{
    public string Id { get; set; } = "";
    public string Json { get; set; } = "";
}
