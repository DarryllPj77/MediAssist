using System.Security.Claims;
using MediAssist.Web.Data;
using MediAssist.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace MediAssist.Web.Services;

/// <summary>Who is acting right now (M3.4b): the signed-in account, as the shared workflow sees it.</summary>
public interface ICurrentActor
{
    Task<Actor> GetAsync();
}

/// <summary>The signed-in account from its claims; signed out gives <see cref="Actor.Nobody"/>, which no action accepts.</summary>
public sealed class ClaimsCurrentActor(AuthenticationStateProvider auth) : ICurrentActor
{
    public async Task<Actor> GetAsync()
    {
        var user = (await auth.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true) return Actor.Nobody;
        var role = Roles.All.FirstOrDefault(user.IsInRole) ?? "";
        return new Actor(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", RunnerSession.FullName(user), role);
    }
}
