using MediAssist.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace MediAssist.Web.Data;

/// <summary>
/// Creates the roles and the fictional demo accounts (M3.4). Runs at start-up in Development only,
/// with the demo password from configuration (<c>Seed:DemoPassword</c>, set in
/// appsettings.Development.json). Safe to run again: existing roles and accounts are left alone.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>The demo accounts: the patient who owns today's household, the administrator, and the four runners.</summary>
    public static IReadOnlyList<(string Email, string FirstName, string LastName, string Role)> Accounts { get; } =
    [
        ("maria.santos@example.com", "Maria", "Santos", Roles.Patient),
        ("ana.reyes@example.com", "Ana", "Reyes", Roles.Administrator),
        .. StaffDecisionRules.Runners.Select(r =>
        {
            var parts = r.Name.Split(' ', 2);
            return ($"{parts[0].ToLowerInvariant()}.{parts[1].ToLowerInvariant()}@example.com", parts[0], parts[1], Roles.Runner);
        }),
    ];

    public static async Task SeedAsync(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, string password)
    {
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
                Check(await roles.CreateAsync(new IdentityRole(role)), role);

        foreach (var (email, first, last, role) in Accounts)
        {
            if (await users.FindByEmailAsync(email) is not null) continue;
            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true, FirstName = first, LastName = last,
                LockoutEnabled = true,
            };
            Check(await users.CreateAsync(user, password), email);
            Check(await users.AddToRoleAsync(user, role), email);
        }
    }

    private static void Check(IdentityResult result, string what)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed {what}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
