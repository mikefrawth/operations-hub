using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Requests;

namespace OperationsHub.Infrastructure.Identity;

public sealed class EntityFrameworkTechnicianDirectory : ITechnicianDirectory
{
    private readonly Persistence.OperationsHubDbContext database;
    private readonly TimeProvider timeProvider;

    public EntityFrameworkTechnicianDirectory(
        Persistence.OperationsHubDbContext database,
        TimeProvider timeProvider)
    {
        this.database = database;
        this.timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<TechnicianDto>> GetActiveTechniciansAsync(CancellationToken cancellationToken)
    {
        var asOfUtc = timeProvider.GetUtcNow();
        return await (
            from user in database.Users.AsNoTracking()
            join userRole in database.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in database.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == RoleNames.Technician &&
                  (user.LockoutEnd == null || user.LockoutEnd <= asOfUtc)
            orderby user.DisplayName, user.Email
            select new TechnicianDto(user.Id, user.DisplayName, user.Email!))
        .ToListAsync(cancellationToken);
    }
}
