using OperationsHub.Application.Requests;
using OperationsHub.Web.Authentication;

namespace OperationsHub.Web.Api;

public static class ServiceRequestEndpoints
{
    public const string RequestClassificationRateLimitPolicy = "request-classification";

    public static IEndpointRouteBuilder MapServiceRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/requests").RequireAuthorization(AuthorizationPolicies.OperationsUser);
        group.MapGet("/", SearchAsync);
        group.MapGet("/open-summary", GetOpenRequestSummariesAsync);
        group.MapPost("/classification", ClassifyAsync)
            .RequireAntiforgeryValidation()
            .RequireRateLimiting(RequestClassificationRateLimitPolicy);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync).RequireAntiforgeryValidation();
        group.MapPut("/{id:guid}", UpdateAsync).RequireAntiforgeryValidation();
        group.MapPost("/{id:guid}/assignments", AssignAsync).RequireAntiforgeryValidation();
        group.MapPost("/{id:guid}/status", ChangeStatusAsync).RequireAntiforgeryValidation();
        group.MapPost("/{id:guid}/comments", AddCommentAsync).RequireAntiforgeryValidation();
        return endpoints;
    }

    private static Task<PagedResult<ServiceRequestListItemDto>> SearchAsync(HttpContext context, [AsParameters] ServiceRequestSearchQuery query, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => service.SearchAsync(RequestActorClaimsMapper.Create(context.User), query, cancellationToken);
    private static async Task<IResult> GetOpenRequestSummariesAsync(HttpContext context, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.GetOpenRequestSummariesAsync(RequestActorClaimsMapper.Create(context.User), cancellationToken));
    private static async Task<IResult> GetAsync(HttpContext context, Guid id, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.GetAsync(RequestActorClaimsMapper.Create(context.User), id, cancellationToken));
    private static async Task<IResult> ClassifyAsync(HttpContext context, RequestClassificationCommand command, IRequestClassificationService service, CancellationToken cancellationToken) => ToResult(await service.ClassifyAsync(RequestActorClaimsMapper.Create(context.User), command, cancellationToken));
    private static async Task<IResult> CreateAsync(HttpContext context, CreateServiceRequestCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.CreateAsync(RequestActorClaimsMapper.Create(context.User), command, cancellationToken), StatusCodes.Status201Created);
    private static async Task<IResult> UpdateAsync(HttpContext context, Guid id, UpdateServiceRequestCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.UpdateAsync(RequestActorClaimsMapper.Create(context.User), id, command, cancellationToken));
    private static async Task<IResult> AssignAsync(HttpContext context, Guid id, AssignServiceRequestCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.AssignAsync(RequestActorClaimsMapper.Create(context.User), id, command, cancellationToken));
    private static async Task<IResult> ChangeStatusAsync(HttpContext context, Guid id, ChangeServiceRequestStatusCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.ChangeStatusAsync(RequestActorClaimsMapper.Create(context.User), id, command, cancellationToken));
    private static async Task<IResult> AddCommentAsync(HttpContext context, Guid id, AddRequestCommentCommand command, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.AddCommentAsync(RequestActorClaimsMapper.Create(context.User), id, command, cancellationToken));

    private static IResult ToResult<T>(RequestOperationResult<T> result, int successStatus = StatusCodes.Status200OK) => result.Status switch
    {
        RequestOperationStatus.Success when successStatus == StatusCodes.Status201Created => Results.Created($"/api/requests/{GetId(result.Value)}", result.Value),
        RequestOperationStatus.Success => Results.Ok(result.Value),
        RequestOperationStatus.ValidationFailed => Results.ValidationProblem(result.Errors),
        RequestOperationStatus.NotFound => Results.NotFound(),
        RequestOperationStatus.Forbidden => Results.Forbid(),
        RequestOperationStatus.Conflict => Results.Conflict(result.Errors),
        RequestOperationStatus.RateLimited => Results.Json(result.Errors, statusCode: StatusCodes.Status429TooManyRequests),
        _ => Results.Problem(),
    };

    private static Guid GetId<T>(T? value) => value is ServiceRequestDetailDto request ? request.Id : throw new InvalidOperationException("Created request has no ID.");
}
