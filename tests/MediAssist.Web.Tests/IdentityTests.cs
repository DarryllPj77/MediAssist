using System.Security.Claims;
using MediAssist.Web.Data;
using MediAssist.Web.Navigation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediAssist.Web.Tests;

/// <summary>Sign-in accounts and roles (M3.4): the migration, Development seeding, and role-to-portal mapping.</summary>
public sealed class IdentityTests : IDisposable
{
    private const string Password = "Test-Only-Password-1!";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"mediassist-identity-test-{Guid.NewGuid():N}.db");

    private ServiceProvider Services() => new ServiceCollection()
        .AddLogging()
        .AddDbContext<MediAssistIdentityDbContext>(o => o.UseSqlite($"Data Source={_path};Pooling=False"))
        .AddIdentityCore<ApplicationUser>(o => o.User.RequireUniqueEmail = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<MediAssistIdentityDbContext>()
        .Services.BuildServiceProvider();

    [Fact]
    public async Task Seeding_creates_the_roles_and_demo_accounts_once()
    {
        await using var services = Services();
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MediAssistIdentityDbContext>().Database.MigrateAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await IdentitySeeder.SeedAsync(users, roles, Password);
        await IdentitySeeder.SeedAsync(users, roles, Password); // again: nothing new

        Assert.Equal(Roles.All.Order(), roles.Roles.Select(r => r.Name!).AsEnumerable().Order());
        Assert.Equal(IdentitySeeder.Accounts.Count, users.Users.Count());
        Assert.Equal(2, (await users.GetUsersInRoleAsync(Roles.Patient)).Count);
        Assert.Single(await users.GetUsersInRoleAsync(Roles.Administrator));
        Assert.Equal(4, (await users.GetUsersInRoleAsync(Roles.Runner)).Count);

        var cora = await users.FindByEmailAsync("cora.bautista@example.com");
        Assert.Equal("Cora Bautista", cora!.FullName);
        Assert.True(await users.CheckPasswordAsync(cora, Password));
        Assert.True(cora.LockoutEnabled);
    }

    [Theory]
    [InlineData(Roles.Patient, "/patient")]
    [InlineData(Roles.Administrator, "/admin")]
    [InlineData(Roles.Runner, "/runner")]
    public void Each_role_opens_its_own_portal(string role, string home)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));
        Assert.Equal(home, RoleNavigation.ForUser(user).Home.Href);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_path)) File.Delete(_path);
    }
}
