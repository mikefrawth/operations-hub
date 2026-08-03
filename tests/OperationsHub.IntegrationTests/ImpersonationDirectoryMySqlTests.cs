using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.IntegrationTests;

public sealed class ImpersonationDirectoryMySqlTests
{
    [Fact]
    public async Task DirectoryReturnsOnlyActiveSingleRoleNonAdministratorUsers()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var options = new DbContextOptionsBuilder<OperationsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        await using var context = new OperationsHubDbContext(options);
        await context.Database.MigrateAsync(CancellationToken.None);
        await using var transaction =
            await context.Database.BeginTransactionAsync(CancellationToken.None);
        var suffix = Guid.NewGuid().ToString("N");
        var eligibleId = Guid.NewGuid().ToString();
        var lockedId = Guid.NewGuid().ToString();
        var administratorId = Guid.NewGuid().ToString();
        var multipleRolesId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        context.Users.AddRange(
            CreateUser(eligibleId, $"eligible-{suffix}@operationshub.local", "Eligible Manager"),
            CreateUser(
                lockedId,
                $"locked-{suffix}@operationshub.local",
                "Locked Requester",
                now.AddMinutes(10)),
            CreateUser(
                administratorId,
                $"administrator-{suffix}@operationshub.local",
                "Another Administrator"),
            CreateUser(
                multipleRolesId,
                $"multiple-{suffix}@operationshub.local",
                "Multiple Roles"));
        context.UserRoles.AddRange(
            CreateUserRole(eligibleId, RoleNames.Manager),
            CreateUserRole(lockedId, RoleNames.Requester),
            CreateUserRole(administratorId, RoleNames.Administrator),
            CreateUserRole(multipleRolesId, RoleNames.Requester),
            CreateUserRole(multipleRolesId, RoleNames.Manager));
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = new EntityFrameworkImpersonationStore(context);
        var targets = await directory.GetEligibleTargetsAsync(now, CancellationToken.None);
        var eligibleTarget = await directory.FindEligibleTargetAsync(
            eligibleId,
            now,
            CancellationToken.None);
        var administratorIsActive = await directory.IsActiveAdministratorAsync(
            administratorId,
            now,
            CancellationToken.None);

        Assert.Contains(
            targets,
            target => target.UserId == eligibleId && target.Role == RoleNames.Manager);
        Assert.DoesNotContain(targets, target => target.UserId == lockedId);
        Assert.DoesNotContain(targets, target => target.UserId == administratorId);
        Assert.DoesNotContain(targets, target => target.UserId == multipleRolesId);
        Assert.NotNull(eligibleTarget);
        Assert.True(administratorIsActive);

        await transaction.RollbackAsync(CancellationToken.None);
    }

    private static ApplicationUser CreateUser(
        string id,
        string email,
        string displayName,
        DateTimeOffset? lockoutEnd = null) =>
        new()
        {
            Id = id,
            UserName = email,
            Email = email,
            DisplayName = displayName,
            LockoutEnabled = true,
            LockoutEnd = lockoutEnd,
        };

    private static IdentityUserRole<string> CreateUserRole(string userId, string role) =>
        new()
        {
            UserId = userId,
            RoleId = role.ToUpperInvariant(),
        };
}
