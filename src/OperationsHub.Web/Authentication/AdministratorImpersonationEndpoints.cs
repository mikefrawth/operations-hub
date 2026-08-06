using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OperationsHub.Application.Impersonation;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Web.Api;

namespace OperationsHub.Web.Authentication;

public static class AdministratorImpersonationEndpoints
{
    public static IEndpointRouteBuilder MapAdministratorImpersonationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/administrator-impersonation/start", StartAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .RequireAuthorization(AuthorizationPolicies.AdministratorImpersonation)
            .ExcludeFromDescription();
        endpoints.MapPost("/administrator-impersonation/end", EndAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .RequireAuthorization()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> StartAsync(
        [FromForm] StartImpersonationInput input,
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAdministratorImpersonationService impersonationService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var administratorUserId = userManager.GetUserId(context.User);
        if (string.IsNullOrWhiteSpace(administratorUserId)
            || string.IsNullOrWhiteSpace(input.TargetUserId))
        {
            return Results.Forbid();
        }

        var administrator = await userManager.FindByIdAsync(administratorUserId);
        var targetUser = await userManager.FindByIdAsync(input.TargetUserId);
        if (administrator is null || targetUser is null)
        {
            return InvalidTarget();
        }

        var result = await impersonationService.StartAsync(
            administratorUserId,
            targetUser.Id,
            CreateRequestContext(context),
            cancellationToken);
        if (result.Status == ImpersonationOperationStatus.Forbidden)
        {
            return Results.Forbid();
        }

        if (!result.Succeeded
            || result.Target is null
            || !string.Equals(result.Target.UserId, targetUser.Id, StringComparison.Ordinal))
        {
            return InvalidTarget();
        }

        var targetPrincipal = await signInManager.CreateUserPrincipalAsync(targetUser);
        SetEffectiveRole(targetPrincipal, result.Target.Role);
        AddImpersonationClaims(
            targetPrincipal,
            administrator,
            result.Target.DisplayName,
            timeProvider.GetUtcNow());

        await context.SignInAsync(
            IdentityConstants.ApplicationScheme,
            targetPrincipal,
            new AuthenticationProperties { IsPersistent = false });

        return Results.Redirect("/");
    }

    private static async Task<IResult> EndAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAdministratorImpersonationService impersonationService,
        CancellationToken cancellationToken)
    {
        var administratorUserId = context.User.FindFirstValue(
            ImpersonationClaimTypes.OriginalAdministratorId);
        var targetUserId = userManager.GetUserId(context.User);
        if (string.IsNullOrWhiteSpace(administratorUserId)
            || string.IsNullOrWhiteSpace(targetUserId))
        {
            return Results.BadRequest();
        }

        var administrator = await userManager.FindByIdAsync(administratorUserId);
        if (administrator is null)
        {
            return Results.Forbid();
        }

        var administratorPrincipal = await signInManager.CreateUserPrincipalAsync(administrator);
        if (!administratorPrincipal.IsInRole(RoleNames.Administrator))
        {
            return Results.Forbid();
        }

        var result = await impersonationService.EndAsync(
            administratorUserId,
            targetUserId,
            CreateRequestContext(context),
            cancellationToken);
        if (!result.Succeeded)
        {
            return result.Status == ImpersonationOperationStatus.Forbidden
                ? Results.Forbid()
                : Results.BadRequest();
        }

        await context.SignInAsync(
            IdentityConstants.ApplicationScheme,
            administratorPrincipal,
            new AuthenticationProperties { IsPersistent = false });

        return Results.Redirect("/");
    }

    private static void SetEffectiveRole(ClaimsPrincipal principal, string role)
    {
        var identity = principal.Identities.First(identity => identity.IsAuthenticated);
        foreach (var roleClaim in identity.Claims
                     .Where(claim => claim.Type == identity.RoleClaimType)
                     .ToList())
        {
            identity.RemoveClaim(roleClaim);
        }

        identity.AddClaim(new Claim(identity.RoleClaimType, role));
    }

    private static void AddImpersonationClaims(
        ClaimsPrincipal targetPrincipal,
        ApplicationUser administrator,
        string targetDisplayName,
        DateTimeOffset startedAtUtc)
    {
        var identity = targetPrincipal.Identities.First(identity => identity.IsAuthenticated);
        identity.AddClaims(
        [
            new Claim(ImpersonationClaimTypes.OriginalAdministratorId, administrator.Id),
            new Claim(
                ImpersonationClaimTypes.OriginalAdministratorDisplayName,
                administrator.DisplayName),
            new Claim(ImpersonationClaimTypes.TargetDisplayName, targetDisplayName),
            new Claim(
                ImpersonationClaimTypes.StartedAtUtc,
                startedAtUtc.ToString("O", CultureInfo.InvariantCulture)),
        ]);
    }

    private static ImpersonationRequestContext CreateRequestContext(HttpContext context) =>
        new(context.Connection.RemoteIpAddress?.ToString(), context.TraceIdentifier);

    private static IResult InvalidTarget() =>
        Results.Redirect("/administration/impersonation?error=invalid-target");

    private sealed class StartImpersonationInput
    {
        public string? TargetUserId { get; init; }
    }
}
