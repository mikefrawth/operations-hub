using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.ReferenceData;

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
            })
            .AddEntityFrameworkStores<OperationsHubDbContext>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/sign-in";
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

        return services;
    }

    public static async Task ApplyOperationsHubMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        await database.Database.MigrateAsync();
    }
}
