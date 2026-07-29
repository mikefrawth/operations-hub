namespace OperationsHub.Application.ReferenceData;

public interface IReferenceDataAdministrationService
{
    public Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(bool activeOnly, CancellationToken cancellationToken);

    public Task<IReadOnlyList<RequestTypeDto>> GetRequestTypesAsync(bool activeOnly, CancellationToken cancellationToken);

    public Task<ReferenceDataOperationResult<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentCommand command, CancellationToken cancellationToken);

    public Task<ReferenceDataOperationResult<DepartmentDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentCommand command, CancellationToken cancellationToken);

    public Task<ReferenceDataOperationResult<DepartmentDto>> DeactivateDepartmentAsync(Guid id, CancellationToken cancellationToken);

    public Task<ReferenceDataOperationResult<RequestTypeDto>> CreateRequestTypeAsync(CreateRequestTypeCommand command, CancellationToken cancellationToken);

    public Task<ReferenceDataOperationResult<RequestTypeDto>> UpdateRequestTypeAsync(Guid id, UpdateRequestTypeCommand command, CancellationToken cancellationToken);

    public Task<ReferenceDataOperationResult<RequestTypeDto>> DeactivateRequestTypeAsync(Guid id, CancellationToken cancellationToken);
}
