using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence;

internal static class DevelopmentIdentitySeeder
{
    private const string DemoPassword = "OperationsHub!2026";

    private static readonly DemoUser[] DemoUsers =
    [
        new("10000000-0000-4000-8000-000000000001", "requester@operationshub.local", "Demo Requester", RoleNames.Requester),
        new("10000000-0000-4000-8000-000000000002", "technician@operationshub.local", "Demo Technician", RoleNames.Technician),
        new("10000000-0000-4000-8000-000000000003", "manager@operationshub.local", "Demo Manager", RoleNames.Manager),
        new("10000000-0000-4000-8000-000000000004", "administrator@operationshub.local", "Demo Administrator", RoleNames.Administrator),
    ];

    internal static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var demoUser in DemoUsers)
        {
            var user = await userManager.FindByEmailAsync(demoUser.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = demoUser.Id,
                    UserName = demoUser.Email,
                    Email = demoUser.Email,
                    EmailConfirmed = true,
                    DisplayName = demoUser.DisplayName,
                    LockoutEnabled = true,
                };

                EnsureSucceeded(await userManager.CreateAsync(user, DemoPassword), $"create development user '{demoUser.Email}'");
            }
            else if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                EnsureSucceeded(await userManager.UpdateAsync(user), $"enable lockout for development user '{demoUser.Email}'");
            }

            if (!await userManager.HasPasswordAsync(user))
            {
                EnsureSucceeded(await userManager.AddPasswordAsync(user, DemoPassword), $"set the development password for '{demoUser.Email}'");
                EnsureSucceeded(await userManager.SetLockoutEndDateAsync(user, null), $"unlock development user '{demoUser.Email}'");
                EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user), $"reset failed access for development user '{demoUser.Email}'");
            }

            if (!await userManager.IsInRoleAsync(user, demoUser.Role))
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(user, demoUser.Role), $"assign role '{demoUser.Role}' to development user '{demoUser.Email}'");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Unable to {operation}. {errors}");
    }

    private sealed record DemoUser(string Id, string Email, string DisplayName, string Role);
}
