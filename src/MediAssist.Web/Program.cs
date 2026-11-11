using MediAssist.Web.Components;
using MediAssist.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Prototype data: fictional records only. Replace with an EF Core implementation later.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPatientPortalDataService, MockPatientPortalDataService>();
builder.Services.AddScoped<IStaffPortalDataService, MockStaffPortalDataService>();
builder.Services.AddScoped<PortalLayoutInterop>();

var app = builder.Build();

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
