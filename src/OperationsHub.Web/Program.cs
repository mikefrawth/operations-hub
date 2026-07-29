using OperationsHub.Web.Components;
using OperationsHub.Web.Api;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Administrator, policy => policy.RequireRole("Administrator"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IReferenceDataAdministrationService, ReferenceDataAdministrationService>();
builder.Services.AddOperationsHubPersistence(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyOperationsHubMigrationsAsync();
}

app.MapStaticAssets();
app.MapReferenceDataEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
