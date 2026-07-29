using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.IntegrationTests;

public sealed class MySqlMigrationTests
{
    [Fact]
    public async Task InitialMigrationAppliesAndSeedsIdentityAndReferenceData()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only";
        var options = new DbContextOptionsBuilder<OperationsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        await using var context = new OperationsHubDbContext(options);
        await context.Database.MigrateAsync(CancellationToken.None);

        var roles = await context.Roles.Select(role => role.Name).ToListAsync(CancellationToken.None);
        var users = await context.Users.Select(user => user.Email).ToListAsync(CancellationToken.None);

        Assert.Contains(RoleNames.Requester, roles);
        Assert.Contains(RoleNames.Technician, roles);
        Assert.Contains(RoleNames.Manager, roles);
        Assert.Contains(RoleNames.Administrator, roles);
        Assert.Contains("administrator@operationshub.local", users);
        Assert.Equal(2, await context.Departments.CountAsync(CancellationToken.None));
        Assert.Equal(2, await context.RequestTypes.CountAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SeededAdministratorCredentialsAndRoleAreValidForSignIn()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only";
        var options = new DbContextOptionsBuilder<OperationsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        await using var context = new OperationsHubDbContext(options);
        await context.Database.MigrateAsync(CancellationToken.None);
        var administrator = await context.Users.SingleAsync(
            user => user.Email == "administrator@operationshub.local",
            CancellationToken.None);
        var roleIds = await context.UserRoles
            .Where(userRole => userRole.UserId == administrator.Id)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(CancellationToken.None);
        var passwordHasher = new PasswordHasher<ApplicationUser>();

        var passwordResult = passwordHasher.VerifyHashedPassword(administrator, administrator.PasswordHash!, "OperationsHub!2026");

        Assert.NotEqual(PasswordVerificationResult.Failed, passwordResult);
        Assert.Contains(RoleNames.Administrator.ToUpperInvariant(), roleIds);
    }
}
