using OperationsHub.Application.ReferenceData;

namespace OperationsHub.Web.Api;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reference-data")
            .RequireAuthorization(AuthorizationPolicies.Administrator);

        group.MapGet("/departments", GetDepartmentsAsync);
        group.MapPost("/departments", CreateDepartmentAsync);
        group.MapPut("/departments/{id:guid}", UpdateDepartmentAsync);
        group.MapPost("/departments/{id:guid}/deactivate", DeactivateDepartmentAsync);

        group.MapGet("/request-types", GetRequestTypesAsync);
        group.MapPost("/request-types", CreateRequestTypeAsync);
        group.MapPut("/request-types/{id:guid}", UpdateRequestTypeAsync);
        group.MapPost("/request-types/{id:guid}/deactivate", DeactivateRequestTypeAsync);

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

    private static IResult ToResult<T>(ReferenceDataOperationResult<T> result) => result.Status switch
    {
        ReferenceDataOperationStatus.Success => Results.Ok(result.Value),
        ReferenceDataOperationStatus.ValidationFailed => Results.ValidationProblem(result.Errors, statusCode: StatusCodes.Status400BadRequest),
        ReferenceDataOperationStatus.Conflict => Results.Conflict(new { errors = result.Errors }),
        ReferenceDataOperationStatus.NotFound => Results.NotFound(),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
    };
}
