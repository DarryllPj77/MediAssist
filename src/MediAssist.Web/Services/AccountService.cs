using MediAssist.Web.Data;
using MediAssist.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace MediAssist.Web.Services;

/// <summary>The signed-in patient's own account details (M3.4b): name on the sign-in account, email read-only.</summary>
public interface IAccountService
{
    Task<PortalUser> GetCurrentAsync();

    /// <summary>Change the account's first and last name; the "You" patient profile follows.</summary>
    Task<AccountResult> SaveNameAsync(string? firstName, string? lastName);
}

/// <summary>
/// Reads and saves the name on the Identity account, and keeps the "You" patient profile (still in
/// the browser until M3.5) in step with it — also when a browser that hasn't seen a rename loads.
/// </summary>
public sealed class IdentityAccountService(
    UserManager<ApplicationUser> users, AuthenticationStateProvider auth, ApplicationWorkspace workspace, MockPortalStore store)
    : IAccountService
{
    public async Task<PortalUser> GetCurrentAsync()
    {
        if (await CurrentUserAsync() is not { } user) return store.CurrentUser;
        if (workspace.IsLoaded) await workspace.SyncSelfNameAsync(user.FullName);
        return store.CurrentUser with { FirstName = user.FirstName, LastName = user.LastName, Email = user.Email };
    }

    public async Task<AccountResult> SaveNameAsync(string? firstName, string? lastName)
    {
        var errors = AccountRules.Validate(firstName, lastName, null);
        if (errors.Count > 0) return AccountResult.Failed(errors);
        if (await CurrentUserAsync() is not { } user) return AccountResult.SaveFailed();

        user.FirstName = firstName!.Trim();
        user.LastName = lastName!.Trim();
        if (!(await users.UpdateAsync(user)).Succeeded) return AccountResult.SaveFailed();

        await workspace.SyncSelfNameAsync(user.FullName);
        return AccountResult.Saved(new AccountDetails(user.FirstName, user.LastName, user.Email));
    }

    // Loaded once per connection: the layout and a page may ask at the same time, and one database
    // context can't run two queries at once. Saving updates this same tracked instance.
    private Task<ApplicationUser?>? _user;

    private Task<ApplicationUser?> CurrentUserAsync() => _user ??= LoadAsync();

    private async Task<ApplicationUser?> LoadAsync()
    {
        var principal = (await auth.GetAuthenticationStateAsync()).User;
        return principal.Identity?.IsAuthenticated == true ? await users.GetUserAsync(principal) : null;
    }
}
