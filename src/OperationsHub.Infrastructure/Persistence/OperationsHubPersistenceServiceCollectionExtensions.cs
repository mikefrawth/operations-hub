using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using OperationsHub.Application.Impersonation;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Application.Reporting;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.ReferenceData;
using OperationsHub.Infrastructure.Reporting;
using OperationsHub.Infrastructure.Requests;

namespace OperationsHub.Infrastructure.Persistence;

public static class OperationsHubPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddOperationsHubPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OperationsHub")
            ?? throw new InvalidOperationException("Connection string 'OperationsHub' is required.");

        services.AddDbContext<OperationsHubDbContext>(options => options.UseMySQL(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<OperationsHubDbContext>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/sign-in";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IReferenceDataStore, EntityFrameworkReferenceDataStore>();
        services.AddScoped<IServiceRequestStore, EntityFrameworkServiceRequestStore>();
        services.AddScoped<IReportingStore, EntityFrameworkReportingStore>();
        services.AddScoped<ITechnicianDirectory, EntityFrameworkTechnicianDirectory>();
        services.AddScoped<IUserDisplayDirectory, EntityFrameworkUserDisplayDirectory>();
        services.AddScoped<IImpersonationStore, EntityFrameworkImpersonationStore>();
        services.Configure<OpenAiRequestClassificationOptions>(configuration.GetSection(OpenAiRequestClassificationOptions.SectionName));
        services.AddHttpClient<IOptionalRequestClassificationAdvisor, OpenAiRequestClassificationAdvisor>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenAiRequestClassificationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 30));
        });

        return services;
    }

    public static async Task InitializeOperationsHubDevelopmentDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Development database initialization cannot run outside the Development environment.");
        }

        var database = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        await database.Database.MigrateAsync(CancellationToken.None);
        await DevelopmentIdentitySeeder.SeedAsync(scope.ServiceProvider);
        await DevelopmentPortfolioScenarioSeeder.SeedAsync(database, CancellationToken.None);
    }

    public static async Task MigrateOperationsHubDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        await database.Database.MigrateAsync(cancellationToken);
    }
}
