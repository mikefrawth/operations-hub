using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Requests;
using System.Linq.Expressions;

namespace OperationsHub.Infrastructure.Identity;

public sealed class EntityFrameworkUserDisplayDirectory : IUserDisplayDirectory
{
    private const int MaximumUsers = 100;

    private readonly Persistence.OperationsHubDbContext database;

    public EntityFrameworkUserDisplayDirectory(Persistence.OperationsHubDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<UserDisplayDto>> GetByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .Take(MaximumUsers)
            .ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await database.Users
            .AsNoTracking()
            .Where(BuildIdPredicate(ids))
            .OrderBy(user => user.DisplayName)
            .Select(user => new UserDisplayDto(user.Id, user.DisplayName))
            .ToListAsync(cancellationToken);
    }

    // The MySQL provider cannot type-map a parameterized primitive collection here, so build a bounded OR predicate instead.
    private static Expression<Func<ApplicationUser, bool>> BuildIdPredicate(IEnumerable<string> userIds)
    {
        var user = Expression.Parameter(typeof(ApplicationUser), "user");
        var userId = Expression.Property(user, nameof(ApplicationUser.Id));
        Expression body = Expression.Constant(false);
        foreach (var id in userIds)
        {
            body = Expression.OrElse(body, Expression.Equal(userId, Expression.Constant(id)));
        }

        return Expression.Lambda<Func<ApplicationUser, bool>>(body, user);
    }
}
