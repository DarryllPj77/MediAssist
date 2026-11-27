using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediAssist.Web.Data;

/// <summary>
/// A MediAssist sign-in account (M3.4). Every account is fictional. The name is what the portals
/// show; the role (Patient, Administrator or Runner) decides which portal the account may open.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>The three roles. Each opens exactly one portal.</summary>
public static class Roles
{
    public const string Patient = "Patient";
    public const string Administrator = "Administrator";
    public const string Runner = "Runner";

    public static IReadOnlyList<string> All { get; } = [Patient, Administrator, Runner];
}

/// <summary>Claim types added to the signed-in user (see <see cref="AppClaimsFactory"/>).</summary>
public static class AppClaims
{
    public const string FirstName = "mediassist:first_name";
    public const string LastName = "mediassist:last_name";
}

/// <summary>
/// Accounts and roles (M3.4): ASP.NET Core Identity in its own SQLite file
/// (<c>App_Data/mediassist-identity.db</c>), managed with EF Core migrations from the start.
/// The shared workflow store stays separate until it moves to migrations (M3.6).
/// </summary>
public sealed class MediAssistIdentityDbContext(DbContextOptions<MediAssistIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Each patient account's own drafts, profiles and read states (M3.5), one JSON row per account.</summary>
    public DbSet<UserWorkspace> UserWorkspaces => Set<UserWorkspace>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<UserWorkspace>(e =>
        {
            e.HasKey(w => w.UserId);
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserWorkspace>(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(40);
            e.Property(u => u.LastName).HasMaxLength(40);
        });
    }
}

/// <summary>A patient account's saved workspace (M3.5): the <c>WorkspaceSnapshot</c> as JSON.</summary>
public sealed class UserWorkspace
{
    public string UserId { get; set; } = "";
    public string Json { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Adds the account's first and last name to the signed-in user, beside the role claims Identity adds.</summary>
public sealed class AppClaimsFactory(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    Microsoft.Extensions.Options.IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(users, roles, options)
{
    protected override async Task<System.Security.Claims.ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new(AppClaims.FirstName, user.FirstName));
        identity.AddClaim(new(AppClaims.LastName, user.LastName));
        return identity;
    }
}
