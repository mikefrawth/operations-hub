using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.IntegrationTests;

public sealed class MySqlMigrationTests
{
    [Fact]
    public async Task MigrationOnlyInitializationAppliesSchemaOutsideDevelopment()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production,
        });
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:OperationsHub"] = connectionString,
            });
        builder.Services.AddOperationsHubPersistence(builder.Configuration);

        using var host = builder.Build();
        await host.Services.MigrateOperationsHubDatabaseAsync(CancellationToken.None);

        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(CancellationToken.None);

        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task LatestMigrationsApplyWithoutSchemaSeededUserAccounts()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var options = new DbContextOptionsBuilder<OperationsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        await using var context = new OperationsHubDbContext(options);
        await context.Database.MigrateAsync(CancellationToken.None);

        var roles = await context.Roles.Select(role => role.Name).ToListAsync(CancellationToken.None);
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var userSeedData = designTimeModel.FindEntityType(typeof(ApplicationUser))!.GetSeedData();

        Assert.Contains(RoleNames.Requester, roles);
        Assert.Contains(RoleNames.Technician, roles);
        Assert.Contains(RoleNames.Manager, roles);
        Assert.Contains(RoleNames.Administrator, roles);
        Assert.Empty(userSeedData);
        Assert.Contains(await context.Departments.Select(department => department.Id).ToListAsync(CancellationToken.None), id => id == Guid.Parse("20000000-0000-4000-8000-000000000001"));
        Assert.Contains(await context.RequestTypes.Select(requestType => requestType.Id).ToListAsync(CancellationToken.None), id => id == Guid.Parse("30000000-0000-4000-8000-000000000001"));
    }

    [Fact]
    public async Task EveryFreshMigrationStageAvoidsExecutableDemoCredentials()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:OperationsHub"] = connectionString,
            });
        builder.Services.AddOperationsHubPersistence(builder.Configuration);

        using var host = builder.Build();
        await host.Services.InitializeOperationsHubDevelopmentDatabaseAsync();

        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync("0");
            await migrator.MigrateAsync("20260729003606_InitialDatabaseAndIdentity");
            context.ChangeTracker.Clear();

            Assert.False(await context.Users.AnyAsync(CancellationToken.None));
            Assert.False(await context.UserRoles.AnyAsync(CancellationToken.None));

            await migrator.MigrateAsync("20260729101935_RemoveDemoIdentityFromSchemaSeed");
            context.ChangeTracker.Clear();

            Assert.False(await context.Users.AnyAsync(CancellationToken.None));
            Assert.False(await context.UserRoles.AnyAsync(CancellationToken.None));
        }
        finally
        {
            await host.Services.InitializeOperationsHubDevelopmentDatabaseAsync();
        }

        await using var verificationScope = host.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        var administrator = await verificationContext.Users.SingleAsync(
            user => user.Email == "administrator@operationshub.local",
            CancellationToken.None);
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var passwordResult = passwordHasher.VerifyHashedPassword(administrator, administrator.PasswordHash!, "OperationsHub!2026");

        Assert.NotEqual(PasswordVerificationResult.Failed, passwordResult);
        Assert.True(administrator.LockoutEnabled);
        var restoredRoleIds = await verificationContext.UserRoles
            .Where(userRole => userRole.UserId == administrator.Id)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(CancellationToken.None);
        Assert.Contains(RoleNames.Administrator, restoredRoleIds, StringComparer.OrdinalIgnoreCase);
    }
}
