using System.Text.Json;
using MediAssist.Web.Data;
using MediAssist.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MediAssist.Web.Services;

/// <summary>Where one account's <see cref="WorkspaceSnapshot"/> is kept between visits.</summary>
public interface IWorkspaceStorage
{
    /// <summary>The saved snapshot (upgraded to the current version), or null when there is none or it cannot be read.</summary>
    Task<WorkspaceSnapshot?> LoadAsync();

    /// <summary>Save the snapshot; false when it could not be saved (the caller must not report success).</summary>
    Task<bool> SaveAsync(WorkspaceSnapshot snapshot);
}

/// <summary>
/// The signed-in account's own row in the account database (M3.5), replacing the browser's
/// protected localStorage: drafts, profiles and read states follow the account to any browser, and
/// no other account can read them. Display preferences stay in the browser (see UiPreferences).
/// </summary>
public sealed class ServerWorkspaceStorage(
    IDbContextFactory<MediAssistIdentityDbContext> factory, ICurrentActor currentActor, TimeProvider clock,
    ILogger<ServerWorkspaceStorage> logger) : IWorkspaceStorage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<WorkspaceSnapshot?> LoadAsync()
    {
        var actor = await currentActor.GetAsync();
        if (!actor.Is(Roles.Patient)) return null;
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            var row = await db.UserWorkspaces.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == actor.UserId);
            return row is null ? null : JsonSerializer.Deserialize<WorkspaceSnapshot>(row.Json, Json)?.Upgrade();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "A saved workspace could not be read; starting a new one.");
            return null;
        }
    }

    public async Task<bool> SaveAsync(WorkspaceSnapshot snapshot)
    {
        var actor = await currentActor.GetAsync();
        if (!actor.Is(Roles.Patient)) return false;
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            var row = await db.UserWorkspaces.FindAsync(actor.UserId);
            if (row is null) db.UserWorkspaces.Add(row = new UserWorkspace { UserId = actor.UserId });
            row.Json = JsonSerializer.Serialize(snapshot, Json);
            row.UpdatedAt = clock.GetUtcNow();
            await db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.Sqlite.SqliteException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Workspace could not be saved.");
            return false;
        }
    }
}
