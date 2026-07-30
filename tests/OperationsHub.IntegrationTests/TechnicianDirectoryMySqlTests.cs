using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.IntegrationTests;

public sealed class TechnicianDirectoryMySqlTests
{
    [Fact]
    public async Task DirectoryReturnsActiveTechniciansAndExcludesLockedAccounts()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var options = new DbContextOptionsBuilder<OperationsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        await using var context = new OperationsHubDbContext(options);
        await context.Database.MigrateAsync(CancellationToken.None);
        await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);
        var suffix = Guid.NewGuid().ToString("N");
        var activeId = Guid.NewGuid().ToString();
        var lockedId = Guid.NewGuid().ToString();

        context.Users.AddRange(
            new ApplicationUser { Id = activeId, UserName = $"active-{suffix}@operationshub.local", Email = $"active-{suffix}@operationshub.local", DisplayName = "Active Technician", LockoutEnabled = true },
            new ApplicationUser { Id = lockedId, UserName = $"locked-{suffix}@operationshub.local", Email = $"locked-{suffix}@operationshub.local", DisplayName = "Locked Technician", LockoutEnabled = true, LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10) });
        context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = activeId, RoleId = RoleNames.Technician.ToUpperInvariant() },
            new IdentityUserRole<string> { UserId = lockedId, RoleId = RoleNames.Technician.ToUpperInvariant() });
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = new EntityFrameworkTechnicianDirectory(context);
        var technicians = await directory.GetActiveTechniciansAsync(CancellationToken.None);

        Assert.Contains(technicians, technician => technician.Id == activeId && technician.DisplayName == "Active Technician");
        Assert.DoesNotContain(technicians, technician => technician.Id == lockedId);

        await transaction.RollbackAsync(CancellationToken.None);
    }
}
