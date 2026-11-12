using System.Security.Cryptography;
using MediAssist.Web.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace MediAssist.Web.Services;

/// <summary>Where one user's <see cref="WorkspaceSnapshot"/> is kept between visits.</summary>
public interface IWorkspaceStorage
{
    /// <summary>The saved snapshot, or null when there is none or it cannot be read.</summary>
    Task<WorkspaceSnapshot?> LoadAsync();

    Task SaveAsync(WorkspaceSnapshot snapshot);
}

/// <summary>
/// The user's own browser (localStorage), encrypted and tamper-proofed by ASP.NET Core Data
/// Protection. Nothing is held on the server between requests, so no state is shared across users.
/// Only callable once the page is interactive (after the first render).
/// </summary>
public sealed class ProtectedLocalWorkspaceStorage(ProtectedLocalStorage storage, ILogger<ProtectedLocalWorkspaceStorage> logger)
    : IWorkspaceStorage
{
    private const string Key = "mediassist.workspace";

    public async Task<WorkspaceSnapshot?> LoadAsync()
    {
        try
        {
            var result = await storage.GetAsync<WorkspaceSnapshot>(Key);
            return result.Success && result.Value?.Version == WorkspaceSnapshot.CurrentVersion ? result.Value : null;
        }
        catch (CryptographicException ex)
        {
            // Saved with other keys (for example after the server's keys changed): start fresh.
            logger.LogInformation(ex, "Stored workspace could not be decrypted; starting a new one.");
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogInformation(ex, "Stored workspace is in an unknown format; starting a new one.");
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    public async Task SaveAsync(WorkspaceSnapshot snapshot)
    {
        try
        {
            await storage.SetAsync(Key, snapshot);
        }
        catch (JSDisconnectedException)
        {
            // The tab closed mid-save; the in-memory copy is gone with it.
        }
    }
}
