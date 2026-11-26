using System.Security.Claims;
using MediAssist.Web.Data;
using MediAssist.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace MediAssist.Web.Services;

/// <summary>
/// The runner using the runner portal (D10; since M3.4 the signed-in Runner account, replacing the
/// "Viewing as" choice). Matches the account's name to the prototype's fictional runners; an
/// account that is not a known runner gets <see cref="Current"/> = null and sees no errands.
/// </summary>
public sealed class RunnerSession(AuthenticationStateProvider auth)
{
    private Task? _loading;

    public RunnerProfile? Current { get; private set; }

    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        var user = (await auth.GetAuthenticationStateAsync()).User;
        Current = user.IsInRole(Roles.Runner) ? StaffDecisionRules.Runners.FirstOrDefault(r => r.Name == FullName(user)) : null;
    }

    public static string FullName(ClaimsPrincipal user) =>
        $"{user.FindFirstValue(AppClaims.FirstName)} {user.FindFirstValue(AppClaims.LastName)}".Trim();
}
