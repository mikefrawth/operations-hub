using System.Security.Claims;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Web.Authentication;

/// <summary>Maps authenticated Identity claims to the actor used by Application request workflows.</summary>
public static class RequestActorClaimsMapper
{
    public static RequestActor Create(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user ID is missing.");

        var role = principal.IsInRole(RoleNames.Administrator)
            ? RequestActorRole.Administrator
            : principal.IsInRole(RoleNames.Manager)
                ? RequestActorRole.Manager
                : principal.IsInRole(RoleNames.Technician)
                    ? RequestActorRole.Technician
                    : principal.IsInRole(RoleNames.Requester)
                        ? RequestActorRole.Requester
                        : throw new InvalidOperationException("Authenticated user has no recognized OperationsHub role.");

        return new RequestActor(userId, role);
    }
}
