using OperationsHub.Application.Requests;
using OperationsHub.Web.Authentication;

namespace OperationsHub.Web.Api;

public static class ServiceRequestEndpoints
{
    public const string RequestClassificationRateLimitPolicy = "request-classification";

    public static IEndpointRouteBuilder MapServiceRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/requests")
            .RequireAuthorization(AuthorizationPolicies.OperationsUser)
            .WithTags("Service requests")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapGet("/", SearchAsync)
            .WithName("SearchServiceRequests")
            .WithSummary("Search visible service requests")
            .WithDescription("Returns a bounded page scoped to the signed-in user's role: owned requests for requesters, assigned requests for technicians, and all requests for managers or administrators.")
            .Produces<PagedResult<ServiceRequestListItemDto>>();
        group.MapGet("/open-summary", GetOpenRequestSummariesAsync)
            .WithName("GetOpenServiceRequestSummary")
            .WithSummary("Get the open-request reporting summary")
            .WithDescription("Returns a bounded MySQL-view projection for managers and administrators.")
            .Produces<PagedResult<OpenRequestSummaryDto>>();
        group.MapPost("/classification", ClassifyAsync)
            .RequireAntiforgeryValidation()
            .RequireRateLimiting(RequestClassificationRateLimitPolicy)
            .WithName("ClassifyServiceRequest")
            .WithSummary("Suggest request classification")
            .WithDescription("Returns advisory category, priority, and summary suggestions for a requester. The deterministic fallback remains available when no external advisor is configured or it fails.")
            .Produces<RequestClassificationSuggestion>()
            .ProducesValidationProblem()
            .Produces<IReadOnlyDictionary<string, string[]>>(StatusCodes.Status429TooManyRequests);
        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetServiceRequest")
            .WithSummary("Get one visible service request")
            .WithDescription("Returns current request state plus assignment, comment, and status history when the signed-in actor may view it.")
            .Produces<ServiceRequestDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync)
            .RequireAntiforgeryValidation()
            .WithName("CreateServiceRequest")
            .WithSummary("Create a service request")
            .WithDescription("Creates a request owned by the signed-in requester and appends initial status and audit history.")
            .Produces<ServiceRequestDetailDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAntiforgeryValidation()
            .WithName("UpdateServiceRequest")
            .WithSummary("Update requester-owned fields")
            .WithDescription("Updates permitted fields on the signed-in requester's own non-closed request using the supplied optimistic-concurrency version.")
            .Produces<ServiceRequestDetailDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<IReadOnlyDictionary<string, string[]>>(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/assignments", AssignAsync)
            .RequireAntiforgeryValidation()
            .WithName("AssignServiceRequest")
            .WithSummary("Assign an active technician")
            .WithDescription("Assigns an eligible technician as a manager or administrator through the transactional MySQL procedure and supplied concurrency version.")
            .Produces<ServiceRequestDetailDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<IReadOnlyDictionary<string, string[]>>(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/status", ChangeStatusAsync)
            .RequireAntiforgeryValidation()
            .WithName("ChangeServiceRequestStatus")
            .WithSummary("Change workflow status")
            .WithDescription("Changes status as a manager, administrator, or assigned technician using the supplied concurrency version and appends status and audit history.")
            .Produces<ServiceRequestDetailDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<IReadOnlyDictionary<string, string[]>>(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/comments", AddCommentAsync)
            .RequireAntiforgeryValidation()
            .WithName("AddServiceRequestComment")
            .WithSummary("Add a request comment")
            .WithDescription("Adds a comment and audit event when the signed-in actor may view the request.")
            .Produces<ServiceRequestDetailDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static Task<PagedResult<ServiceRequestListItemDto>> SearchAsync(HttpContext context, [AsParameters] ServiceRequestSearchQuery query, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => service.SearchAsync(RequestActorClaimsMapper.Create(context.User), query, cancellationToken);
    private static async Task<IResult> GetOpenRequestSummariesAsync(HttpContext context, [AsParameters] OpenRequestSummaryQuery query, IServiceRequestWorkflowService service, CancellationToken cancellationToken) => ToResult(await service.GetOpenRequestSummariesAsync(RequestActorClaimsMapper.Create(context.User), query, cancellationToken));
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
