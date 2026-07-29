using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence;

internal static class DevelopmentSeedData
{
    internal const string RequesterUserId = "10000000-0000-4000-8000-000000000001";
    internal const string TechnicianUserId = "10000000-0000-4000-8000-000000000002";
    internal const string ManagerUserId = "10000000-0000-4000-8000-000000000003";
    internal const string AdministratorUserId = "10000000-0000-4000-8000-000000000004";

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

        var users = new[]
        {
            CreateUser(RequesterUserId, "requester@operationshub.local", "Demo Requester", "AQAAAAIAAYagAAAAEMWG1AkO1GvM7MAvuvUkbCR+Cpvj6T7Tsl1ncuhgnJh9xdXfQCihdQQydyF+Aoe1Bw=="),
            CreateUser(TechnicianUserId, "technician@operationshub.local", "Demo Technician", "AQAAAAIAAYagAAAAEKqXzGSJ4LCc4rgGriOFk/Xc7bs17cZqQ3C2/gt8XMYY6dnLxhKpTEaKj9vvWQb9bw=="),
            CreateUser(ManagerUserId, "manager@operationshub.local", "Demo Manager", "AQAAAAIAAYagAAAAEIFCG1AJJI1TJ71/8LzrLcFdYDBixbAa/xRqEG2BKnuG7DzncTJyDNjMEnJLggm81w=="),
            CreateUser(AdministratorUserId, "administrator@operationshub.local", "Demo Administrator", "AQAAAAIAAYagAAAAENBuiU0xPMDBnGChDta1qRgLqc6Lsp7pxVIZz52nUIb1pZOGsmQaUYV42Z/pmCtAmg=="),
        };
        builder.Entity<ApplicationUser>().HasData(users);
        builder.Entity<IdentityUserRole<string>>().HasData(
            CreateUserRole(RequesterUserId, RoleNames.Requester),
            CreateUserRole(TechnicianUserId, RoleNames.Technician),
            CreateUserRole(ManagerUserId, RoleNames.Manager),
            CreateUserRole(AdministratorUserId, RoleNames.Administrator));

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

    private static ApplicationUser CreateUser(string id, string email, string displayName, string passwordHash)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = displayName,
            SecurityStamp = id,
            ConcurrencyStamp = id,
        };
        user.PasswordHash = passwordHash;
        return user;
    }

    private static IdentityUserRole<string> CreateUserRole(string userId, string roleName) => new()
    {
        UserId = userId,
        RoleId = roleName.ToUpperInvariant(),
    };
}
