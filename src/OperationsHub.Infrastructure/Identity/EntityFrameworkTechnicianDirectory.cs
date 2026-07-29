using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Requests;

namespace OperationsHub.Infrastructure.Identity;

public sealed class EntityFrameworkTechnicianDirectory : ITechnicianDirectory
{
    private readonly Persistence.OperationsHubDbContext database;

    public EntityFrameworkTechnicianDirectory(Persistence.OperationsHubDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<TechnicianDto>> GetActiveTechniciansAsync(CancellationToken cancellationToken) =>
        await (
            from user in database.Users.AsNoTracking()
            join userRole in database.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in database.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == RoleNames.Technician && user.LockoutEnd == null
            orderby user.DisplayName, user.Email
            select new TechnicianDto(user.Id, user.DisplayName, user.Email!))
        .ToListAsync(cancellationToken);
}
