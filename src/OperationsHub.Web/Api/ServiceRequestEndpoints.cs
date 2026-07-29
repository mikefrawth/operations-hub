using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Web.Api;

public static class ServiceRequestEndpoints
{
    public static IEndpointRouteBuilder MapServiceRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/requests").RequireAuthorization();
        group.MapGet("/", SearchAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        group.MapPut("/{id:guid}", UpdateAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        group.MapPost("/{id:guid}/assignments", AssignAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        group.MapPost("/{id:guid}/status", ChangeStatusAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        group.MapPost("/{id:guid}/comments", AddCommentAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        return endpoints;
    }

    private static Task<PagedResult<ServiceRequestListItemDto>> SearchAsync(HttpContext context, [AsParameters] ServiceRequestSearchQuery query, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => service.SearchAsync(GetActor(context.User), query, cancellationToken);
    private static async Task<IResult> GetAsync(HttpContext context, Guid id, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.GetAsync(GetActor(context.User), id, cancellationToken));
    private static async Task<IResult> CreateAsync(HttpContext context, CreateServiceRequestCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.CreateAsync(GetActor(context.User), command, cancellationToken), StatusCodes.Status201Created);
    private static async Task<IResult> UpdateAsync(HttpContext context, Guid id, UpdateServiceRequestCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.UpdateAsync(GetActor(context.User), id, command, cancellationToken));
    private static async Task<IResult> AssignAsync(HttpContext context, Guid id, AssignServiceRequestCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.AssignAsync(GetActor(context.User), id, command, cancellationToken));
    private static async Task<IResult> ChangeStatusAsync(HttpContext context, Guid id, ChangeServiceRequestStatusCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.ChangeStatusAsync(GetActor(context.User), id, command, cancellationToken));
    private static async Task<IResult> AddCommentAsync(HttpContext context, Guid id, AddRequestCommentCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.AddCommentAsync(GetActor(context.User), id, command, cancellationToken));

    private static RequestActor GetActor(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user ID is missing.");
        var role = user.IsInRole(RoleNames.Administrator) ? RequestActorRole.Administrator : user.IsInRole(RoleNames.Manager) ? RequestActorRole.Manager : user.IsInRole(RoleNames.Technician) ? RequestActorRole.Technician : RequestActorRole.Requester;
        return new RequestActor(userId, role);
    }

    private static IResult ToResult<T>(RequestOperationResult<T> result, int successStatus = StatusCodes.Status200OK) => result.Status switch
    {
        RequestOperationStatus.Success when successStatus == StatusCodes.Status201Created => Results.Created($"/api/requests/{GetId(result.Value)}", result.Value),
        RequestOperationStatus.Success => Results.Ok(result.Value),
        RequestOperationStatus.ValidationFailed => Results.ValidationProblem(result.Errors),
        RequestOperationStatus.NotFound => Results.NotFound(),
        RequestOperationStatus.Forbidden => Results.Forbid(),
        _ => Results.Problem(),
    };

    private static Guid GetId<T>(T? value) => value is ServiceRequestDetailDto request ? request.Id : throw new InvalidOperationException("Created request has no ID.");
}
