using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using OperationsHub.Web.Api;
using OperationsHub.Web.Authentication;
using OperationsHub.Web.Components;
using OperationsHub.Application.Impersonation;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Application.Reporting;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.Persistence;

const string MigrationSwitch = "--migrate";
var runMigrationsOnly = args.Contains(MigrationSwitch, StringComparer.Ordinal);
var hostArguments = args.Where(argument => !string.Equals(argument, MigrationSwitch, StringComparison.Ordinal)).ToArray();
var builder = WebApplication.CreateBuilder(hostArguments);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("OperationsHub")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

var trustForwardedHeaders = builder.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders");
if (trustForwardedHeaders)
{
    var knownProxies = builder.Configuration
        .GetSection("ReverseProxy:KnownProxies")
        .Get<string[]>()?
        .Select(IPAddress.Parse)
        .ToArray() ?? [];
    if (knownProxies.Length == 0)
    {
        throw new InvalidOperationException("ReverseProxy:KnownProxies must contain at least one IP address when forwarded headers are enabled.");
    }

    // Forwarded client IPs feed throttling and audit records, so trust only explicitly configured hops.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var knownProxy in knownProxies)
        {
            options.KnownProxies.Add(knownProxy);
        }
    });
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OperationsHubDbContext>("database", tags: ["ready"]);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(
        AuthorizationPolicies.Administrator,
        policy => policy.RequireRole(AuthorizationPolicies.Administrator))
    .AddPolicy(
        AuthorizationPolicies.AdministratorImpersonation,
        policy =>
        {
            policy.RequireRole(AuthorizationPolicies.Administrator);
            policy.RequireAssertion(_ => builder.Environment.IsDevelopment());
        })
    .AddPolicy(
        AuthorizationPolicies.ManagerOrAdministrator,
        policy => policy.RequireRole(RoleNames.Manager, RoleNames.Administrator))
    .AddPolicy(
        AuthorizationPolicies.OperationsUser,
        policy => policy.RequireRole(RoleNames.Requester, RoleNames.Technician, RoleNames.Manager, RoleNames.Administrator));
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
    options.AddPolicy(ServiceRequestEndpoints.RequestClassificationRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? $"anonymous:{context.Connection.RemoteIpAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAdministratorImpersonationService, AdministratorImpersonationService>();
builder.Services.AddScoped<IReferenceDataAdministrationService, ReferenceDataAdministrationService>();
builder.Services.AddScoped<IRequestReportingService, RequestReportingService>();
builder.Services.AddScoped<IServiceRequestWorkflowService, ServiceRequestWorkflowService>();
builder.Services.AddScoped<IRequestClassificationService, RequestClassificationService>();
builder.Services.AddSingleton<IRequestClassificationUsageLimiter, InMemoryRequestClassificationUsageLimiter>();
builder.Services.AddOperationsHubPersistence(builder.Configuration);

var app = builder.Build();

if (runMigrationsOnly)
{
    await app.Services.MigrateOperationsHubDatabaseAsync(CancellationToken.None);
    LogDatabaseMigrationsCompleted(app.Logger);
    return;
}

if (trustForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    LogUnhandledRequestFailure(
        app.Logger,
        exception,
        context.Request.Method,
        context.Request.Path.Value ?? "/");

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
        return;
    }

    context.Response.Redirect("/Error");
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseHttpsRedirection());
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
app.UseAuthentication();
// User-partitioned endpoint limiters require authentication to populate HttpContext.User first.
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeOperationsHubDevelopmentDatabaseAsync();
    app.MapAdministratorImpersonationEndpoints();
}

app.MapStaticAssets();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
}).AllowAnonymous();
app.MapAuthenticationEndpoints();
app.MapReferenceDataEndpoints();
app.MapReportingEndpoints();
app.MapServiceRequestEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled request failure for {RequestMethod} {RequestPath}.")]
    private static partial void LogUnhandledRequestFailure(
        ILogger logger,
        Exception? exception,
        string requestMethod,
        string requestPath);

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "OperationsHub database migrations completed successfully.")]
    private static partial void LogDatabaseMigrationsCompleted(ILogger logger);
}
