using MediAssist.Web.Components;
using MediAssist.Web.Data;
using MediAssist.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Prototype data: fictional records only. Replace with an EF Core implementation later.
builder.Services.AddSingleton(TimeProvider.System);
// One shared in-memory store, so every portal reads the same fictional story.
builder.Services.AddSingleton<MockPortalStore>();
// A fixed application name keeps browser data readable when the app moves to another folder
// (D9a; changing it reset earlier prototype data once).
builder.Services.AddDataProtection().SetApplicationName("MediAssist");
// The shared workflow store (D9a): submitted applications, uploads, reviews and their notifications.
// Path from configuration (`Workflow:DatabasePath`, M3.3) so tests can use a throwaway file;
// relative paths are under the content root. Default: App_Data/mediassist.db.
var workflowDb = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Workflow:DatabasePath"] ?? Path.Combine("App_Data", "mediassist.db"));
Directory.CreateDirectory(Path.GetDirectoryName(workflowDb)!);
builder.Services.AddDbContextFactory<WorkflowDbContext>(o => o.UseSqlite($"Data Source={workflowDb}"));
builder.Services.AddSingleton<WorkflowService>();

// Sign-in and roles (M3.4): ASP.NET Core Identity with cookie sign-in, accounts in their own SQLite
// file managed by EF Core migrations (`Identity:DatabasePath`, default App_Data/mediassist-identity.db).
var identityDb = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Identity:DatabasePath"] ?? Path.Combine("App_Data", "mediassist-identity.db"));
Directory.CreateDirectory(Path.GetDirectoryName(identityDb)!);
builder.Services.AddDbContext<MediAssistIdentityDbContext>(o => o.UseSqlite($"Data Source={identityDb}"));
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = IdentityConstants.ApplicationScheme;
        o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.SignIn.RequireConfirmedAccount = false; // no email sending in the prototype
        o.User.RequireUniqueEmail = true;
        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MediAssistIdentityDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<AppClaimsFactory>();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/account/login";
    o.AccessDeniedPath = "/account/access-denied";
    o.Cookie.Name = "MediAssist.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
// Each user's own drafts, profiles and preferences: per circuit, kept in that user's browser storage.
builder.Services.AddScoped<IWorkspaceStorage, ProtectedLocalWorkspaceStorage>();
builder.Services.AddScoped<ApplicationWorkspace>();
builder.Services.AddScoped<IPatientPortalDataService, MockPatientPortalDataService>();
builder.Services.AddScoped<IStaffPortalDataService, MockStaffPortalDataService>();
builder.Services.AddScoped<PortalLayoutInterop>();
builder.Services.AddScoped<UiPreferences>();
builder.Services.AddScoped<RunnerSession>();
builder.Services.AddScoped<ICurrentActor, ClaimsCurrentActor>();
builder.Services.AddScoped<IAccountService, IdentityAccountService>();
builder.Services.AddScoped<PageSectionState>();

var app = builder.Build();

await app.Services.GetRequiredService<WorkflowService>().InitializeAsync();

// Accounts: apply migrations; in Development only, create the roles and fictional demo accounts.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<MediAssistIdentityDbContext>().Database.MigrateAsync();
    if (app.Environment.IsDevelopment() && app.Configuration["Seed:DemoPassword"] is { Length: > 0 } demoPassword)
        await IdentitySeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
            demoPassword);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Sign out (M3.4): a form post with an antiforgery token (the form field makes the framework check it).
app.MapPost("/account/logout", async (SignInManager<ApplicationUser> signIn, [FromForm] string? returnUrl) =>
{
    await signIn.SignOutAsync();
    return Results.LocalRedirect("/account/login?signedOut=true");
});

app.Run();
