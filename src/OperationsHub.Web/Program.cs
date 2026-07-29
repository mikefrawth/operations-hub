using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OperationsHub.Web.Components;
using OperationsHub.Web.Api;
using OperationsHub.Web.Authentication;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddProblemDetails();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Administrator, policy => policy.RequireRole(AuthorizationPolicies.Administrator));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthenticationEndpoints.SignInRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});
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

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "base-uri 'self'; form-action 'self'; frame-ancestors 'none'; object-src 'none'";
    context.Response.Headers.Append("Permissions-Policy", "camera=(), geolocation=(), microphone=()");
    await next();
});

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePages(async statusCodeContext =>
        await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
            .ExecuteAsync(statusCodeContext.HttpContext)));

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeOperationsHubDevelopmentDatabaseAsync();
}

app.MapStaticAssets();
app.MapAuthenticationEndpoints();
app.MapReferenceDataEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
