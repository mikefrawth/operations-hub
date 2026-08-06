using OperationsHub.Application.ReferenceData;

namespace OperationsHub.Web.Api;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reference-data")
            .RequireAuthorization(AuthorizationPolicies.Administrator)
            .WithTags("Reference data")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/departments", GetDepartmentsAsync)
            .WithName("GetDepartments")
            .WithSummary("List departments")
            .WithDescription("Returns only active departments when activeOnly is true; otherwise returns all departments.")
            .Produces<IReadOnlyList<DepartmentDto>>();
        group.MapPost("/departments", CreateDepartmentAsync)
            .RequireAntiforgeryValidation()
            .WithName("CreateDepartment")
            .WithSummary("Create a department")
            .Produces<DepartmentDto>()
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict);
        group.MapPut("/departments/{id:guid}", UpdateDepartmentAsync)
            .RequireAntiforgeryValidation()
            .WithName("UpdateDepartment")
            .WithSummary("Rename a department")
            .Produces<DepartmentDto>()
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/departments/{id:guid}/deactivate", DeactivateDepartmentAsync)
            .RequireAntiforgeryValidation()
            .WithName("DeactivateDepartment")
            .WithSummary("Deactivate a department")
            .WithDescription("Soft-deactivates a department while preserving historical request relationships.")
            .Produces<DepartmentDto>()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/departments/{id:guid}/reactivate", ReactivateDepartmentAsync)
            .RequireAntiforgeryValidation()
            .WithName("ReactivateDepartment")
            .WithSummary("Reactivate a department")
            .Produces<DepartmentDto>()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/request-types", GetRequestTypesAsync)
            .WithName("GetRequestTypes")
            .WithSummary("List request types")
            .WithDescription("Returns only active request types when activeOnly is true; otherwise returns all request types.")
            .Produces<IReadOnlyList<RequestTypeDto>>();
        group.MapPost("/request-types", CreateRequestTypeAsync)
            .RequireAntiforgeryValidation()
            .WithName("CreateRequestType")
            .WithSummary("Create a request type")
            .Produces<RequestTypeDto>()
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict);
        group.MapPut("/request-types/{id:guid}", UpdateRequestTypeAsync)
            .RequireAntiforgeryValidation()
            .WithName("UpdateRequestType")
            .WithSummary("Update a request type")
            .Produces<RequestTypeDto>()
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/request-types/{id:guid}/deactivate", DeactivateRequestTypeAsync)
            .RequireAntiforgeryValidation()
            .WithName("DeactivateRequestType")
            .WithSummary("Deactivate a request type")
            .WithDescription("Soft-deactivates a request type while preserving historical request relationships.")
            .Produces<RequestTypeDto>()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/request-types/{id:guid}/reactivate", ReactivateRequestTypeAsync)
            .RequireAntiforgeryValidation()
            .WithName("ReactivateRequestType")
            .WithSummary("Reactivate a request type")
            .Produces<RequestTypeDto>()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(
        bool activeOnly,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) => service.GetDepartmentsAsync(activeOnly, cancellationToken);

    private static async Task<IResult> CreateDepartmentAsync(
        CreateDepartmentCommand command,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.CreateDepartmentAsync(command, cancellationToken));

    private static async Task<IResult> UpdateDepartmentAsync(
        Guid id,
        UpdateDepartmentCommand command,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.UpdateDepartmentAsync(id, command, cancellationToken));

    private static async Task<IResult> DeactivateDepartmentAsync(
        Guid id,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.DeactivateDepartmentAsync(id, cancellationToken));

    private static async Task<IResult> ReactivateDepartmentAsync(
        Guid id,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.ReactivateDepartmentAsync(id, cancellationToken));

    private static Task<IReadOnlyList<RequestTypeDto>> GetRequestTypesAsync(
        bool activeOnly,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) => service.GetRequestTypesAsync(activeOnly, cancellationToken);

    private static async Task<IResult> CreateRequestTypeAsync(
        CreateRequestTypeCommand command,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.CreateRequestTypeAsync(command, cancellationToken));

    private static async Task<IResult> UpdateRequestTypeAsync(
        Guid id,
        UpdateRequestTypeCommand command,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.UpdateRequestTypeAsync(id, command, cancellationToken));

    private static async Task<IResult> DeactivateRequestTypeAsync(
        Guid id,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.DeactivateRequestTypeAsync(id, cancellationToken));

    private static async Task<IResult> ReactivateRequestTypeAsync(
        Guid id,
        IReferenceDataAdministrationService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.ReactivateRequestTypeAsync(id, cancellationToken));

    private static IResult ToResult<T>(ReferenceDataOperationResult<T> result) => result.Status switch
    {
        ReferenceDataOperationStatus.Success => Results.Ok(result.Value),
        ReferenceDataOperationStatus.ValidationFailed => Results.ValidationProblem(result.Errors, statusCode: StatusCodes.Status400BadRequest),
        ReferenceDataOperationStatus.Conflict => Results.ValidationProblem(
            result.Errors,
            statusCode: StatusCodes.Status409Conflict,
            title: "A reference-data record already uses this value."),
        ReferenceDataOperationStatus.NotFound => Results.NotFound(),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
    };
}
