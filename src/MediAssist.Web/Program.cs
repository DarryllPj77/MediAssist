using MediAssist.Web.Components;
using MediAssist.Web.Data;
using MediAssist.Web.Services;
using Microsoft.AspNetCore.DataProtection;
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
var workflowDb = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "mediassist.db");
Directory.CreateDirectory(Path.GetDirectoryName(workflowDb)!);
builder.Services.AddDbContextFactory<WorkflowDbContext>(o => o.UseSqlite($"Data Source={workflowDb}"));
builder.Services.AddSingleton<WorkflowService>();
// Each user's own drafts, profiles and preferences: per circuit, kept in that user's browser storage.
builder.Services.AddScoped<IWorkspaceStorage, ProtectedLocalWorkspaceStorage>();
builder.Services.AddScoped<ApplicationWorkspace>();
builder.Services.AddScoped<IPatientPortalDataService, MockPatientPortalDataService>();
builder.Services.AddScoped<IStaffPortalDataService, MockStaffPortalDataService>();
builder.Services.AddScoped<PortalLayoutInterop>();
builder.Services.AddScoped<UiPreferences>();
builder.Services.AddScoped<RunnerSession>();
builder.Services.AddScoped<PageSectionState>();

var app = builder.Build();

await app.Services.GetRequiredService<WorkflowService>().InitializeAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
