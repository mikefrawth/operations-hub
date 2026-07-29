using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence;

internal static class InitialSeedData
{
    private static readonly DateTimeOffset SeededAtUtc = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    internal static void Apply(ModelBuilder builder)
    {
        var roles = new[]
        {
            CreateRole(RoleNames.Requester),
            CreateRole(RoleNames.Technician),
            CreateRole(RoleNames.Manager),
            CreateRole(RoleNames.Administrator),
        };
        builder.Entity<IdentityRole>().HasData(roles);

        builder.Entity<Department>().HasData(
            new Department(Guid.Parse("20000000-0000-4000-8000-000000000001"), "Information Technology", SeededAtUtc),
            new Department(Guid.Parse("20000000-0000-4000-8000-000000000002"), "Facilities", SeededAtUtc));
        builder.Entity<RequestType>().HasData(
            new RequestType(Guid.Parse("30000000-0000-4000-8000-000000000001"), "Access request", "Request access to an internal system or service.", SeededAtUtc),
            new RequestType(Guid.Parse("30000000-0000-4000-8000-000000000002"), "Facilities issue", "Report an issue with a workplace or facility.", SeededAtUtc));
    }

    private static IdentityRole CreateRole(string name) => new()
    {
        Id = name.ToUpperInvariant(),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = name.ToUpperInvariant(),
    };
}
