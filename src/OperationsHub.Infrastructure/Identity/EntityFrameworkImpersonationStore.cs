using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Impersonation;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.Infrastructure.Identity;

public sealed class EntityFrameworkImpersonationStore : IImpersonationStore
{
    private const int MaximumTargets = 100;
    private static readonly string[] EligibleRoles =
    [
        RoleNames.Requester,
        RoleNames.Technician,
        RoleNames.Manager,
    ];

    private readonly OperationsHubDbContext database;

    public EntityFrameworkImpersonationStore(OperationsHubDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<ImpersonationTargetDto>> GetEligibleTargetsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken) =>
        await CreateEligibleTargetsQuery(asOfUtc, null)
            .Take(MaximumTargets)
            .ToListAsync(cancellationToken);

    public Task<ImpersonationTargetDto?> FindEligibleTargetAsync(
        string userId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken) =>
        CreateEligibleTargetsQuery(asOfUtc, userId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> IsActiveAdministratorAsync(
        string userId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken) =>
        database.Users.AsNoTracking()
            .Where(user =>
                user.Id == userId
                && (user.LockoutEnd == null || user.LockoutEnd <= asOfUtc))
            .AnyAsync(
                user => database.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(
                        database.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (_, role) => role.Name)
                    .Any(roleName => roleName == RoleNames.Administrator),
                cancellationToken);

    public void Add(AuditEvent auditEvent) => database.AuditEvents.Add(auditEvent);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        database.SaveChangesAsync(cancellationToken);

    private IQueryable<ImpersonationTargetDto> CreateEligibleTargetsQuery(
        DateTimeOffset asOfUtc,
        string? selectedUserId) =>
        from user in database.Users.AsNoTracking()
        join userRole in database.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
        join role in database.Roles.AsNoTracking() on userRole.RoleId equals role.Id
        where (user.LockoutEnd == null || user.LockoutEnd <= asOfUtc)
              && (selectedUserId == null || user.Id == selectedUserId)
              && EligibleRoles.Contains(role.Name!)
              && database.UserRoles.Count(candidateRole => candidateRole.UserId == user.Id) == 1
        orderby role.Name, user.DisplayName
        select new ImpersonationTargetDto(
            user.Id,
            user.DisplayName,
            user.Email ?? user.UserName ?? user.Id,
            role.Name!);
}
