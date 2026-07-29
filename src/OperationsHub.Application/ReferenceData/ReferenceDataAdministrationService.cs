using OperationsHub.Domain.Entities;

namespace OperationsHub.Application.ReferenceData;

public sealed class ReferenceDataAdministrationService : IReferenceDataAdministrationService
{
    private readonly IReferenceDataStore store;
    private readonly TimeProvider timeProvider;

    public ReferenceDataAdministrationService(IReferenceDataStore store, TimeProvider timeProvider)
    {
        this.store = store;
        this.timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var departments = await store.GetDepartmentsAsync(activeOnly, cancellationToken);
        return departments.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<RequestTypeDto>> GetRequestTypesAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var requestTypes = await store.GetRequestTypesAsync(activeOnly, cancellationToken);
        return requestTypes.Select(Map).ToList();
    }

    public async Task<ReferenceDataOperationResult<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var name = ValidateName(command.Name, Department.NameMaximumLength);
        if (!name.Succeeded)
        {
            return Failure<DepartmentDto>(name);
        }

        if (await store.DepartmentNameExistsAsync(name.Value!, null, cancellationToken))
        {
            return Conflict<DepartmentDto>("name", "A department with this name already exists.");
        }

        var department = new Department(Guid.NewGuid(), name.Value!, timeProvider.GetUtcNow());
        store.Add(department);
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(department));
    }

    public async Task<ReferenceDataOperationResult<DepartmentDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var name = ValidateName(command.Name, Department.NameMaximumLength);
        if (!name.Succeeded)
        {
            return Failure<DepartmentDto>(name);
        }

        var department = await store.FindDepartmentAsync(id, cancellationToken);
        if (department is null)
        {
            return NotFound<DepartmentDto>();
        }

        if (await store.DepartmentNameExistsAsync(name.Value!, id, cancellationToken))
        {
            return Conflict<DepartmentDto>("name", "A department with this name already exists.");
        }

        department.Rename(name.Value!);
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(department));
    }

    public async Task<ReferenceDataOperationResult<DepartmentDto>> DeactivateDepartmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await store.FindDepartmentAsync(id, cancellationToken);
        if (department is null)
        {
            return NotFound<DepartmentDto>();
        }

        department.Deactivate();
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(department));
    }

    public async Task<ReferenceDataOperationResult<DepartmentDto>> ReactivateDepartmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await store.FindDepartmentAsync(id, cancellationToken);
        if (department is null)
        {
            return NotFound<DepartmentDto>();
        }

        department.Reactivate();
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(department));
    }

    public async Task<ReferenceDataOperationResult<RequestTypeDto>> CreateRequestTypeAsync(CreateRequestTypeCommand command, CancellationToken cancellationToken)
    {
        var name = ValidateName(command.Name, RequestType.NameMaximumLength);
        var description = ValidateDescription(command.Description);
        if (!name.Succeeded)
        {
            return Failure<RequestTypeDto>(name);
        }

        if (!description.Succeeded)
        {
            return Failure<RequestTypeDto>(description);
        }

        if (await store.RequestTypeNameExistsAsync(name.Value!, null, cancellationToken))
        {
            return Conflict<RequestTypeDto>("name", "A request type with this name already exists.");
        }

        var requestType = new RequestType(Guid.NewGuid(), name.Value!, description.Value, timeProvider.GetUtcNow());
        store.Add(requestType);
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(requestType));
    }

    public async Task<ReferenceDataOperationResult<RequestTypeDto>> UpdateRequestTypeAsync(Guid id, UpdateRequestTypeCommand command, CancellationToken cancellationToken)
    {
        var name = ValidateName(command.Name, RequestType.NameMaximumLength);
        var description = ValidateDescription(command.Description);
        if (!name.Succeeded)
        {
            return Failure<RequestTypeDto>(name);
        }

        if (!description.Succeeded)
        {
            return Failure<RequestTypeDto>(description);
        }

        var requestType = await store.FindRequestTypeAsync(id, cancellationToken);
        if (requestType is null)
        {
            return NotFound<RequestTypeDto>();
        }

        if (await store.RequestTypeNameExistsAsync(name.Value!, id, cancellationToken))
        {
            return Conflict<RequestTypeDto>("name", "A request type with this name already exists.");
        }

        requestType.Update(name.Value!, description.Value);
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(requestType));
    }

    public async Task<ReferenceDataOperationResult<RequestTypeDto>> DeactivateRequestTypeAsync(Guid id, CancellationToken cancellationToken)
    {
        var requestType = await store.FindRequestTypeAsync(id, cancellationToken);
        if (requestType is null)
        {
            return NotFound<RequestTypeDto>();
        }

        requestType.Deactivate();
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(requestType));
    }

    public async Task<ReferenceDataOperationResult<RequestTypeDto>> ReactivateRequestTypeAsync(Guid id, CancellationToken cancellationToken)
    {
        var requestType = await store.FindRequestTypeAsync(id, cancellationToken);
        if (requestType is null)
        {
            return NotFound<RequestTypeDto>();
        }

        requestType.Reactivate();
        await store.SaveChangesAsync(cancellationToken);
        return ReferenceDataOperationResults.Success(Map(requestType));
    }

    private static ReferenceDataOperationResult<string?> ValidateName(string? value, int maximumLength)
    {
        var name = value?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return ReferenceDataOperationResults.Failure<string?>(ReferenceDataOperationStatus.ValidationFailed, "name", "Name is required.");
        }

        if (name.Length > maximumLength)
        {
            return ReferenceDataOperationResults.Failure<string?>(ReferenceDataOperationStatus.ValidationFailed, "name", $"Name cannot exceed {maximumLength} characters.");
        }

        return ReferenceDataOperationResults.Success<string?>(name);
    }

    private static ReferenceDataOperationResult<string?> ValidateDescription(string? value)
    {
        var description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (description?.Length > RequestType.DescriptionMaximumLength)
        {
            return ReferenceDataOperationResults.Failure<string?>(ReferenceDataOperationStatus.ValidationFailed, "description", $"Description cannot exceed {RequestType.DescriptionMaximumLength} characters.");
        }

        return ReferenceDataOperationResults.Success<string?>(description);
    }

    private static ReferenceDataOperationResult<T> Failure<T>(ReferenceDataOperationResult<string?> validationResult) =>
        new(validationResult.Status, default, validationResult.Errors);

    private static ReferenceDataOperationResult<T> Conflict<T>(string field, string message) =>
        ReferenceDataOperationResults.Failure<T>(ReferenceDataOperationStatus.Conflict, field, message);

    private static ReferenceDataOperationResult<T> NotFound<T>() =>
        ReferenceDataOperationResults.Failure<T>(ReferenceDataOperationStatus.NotFound, "id", "The requested record was not found.");

    private static DepartmentDto Map(Department department) => new(department.Id, department.Name, department.IsActive, department.CreatedAtUtc);

    private static RequestTypeDto Map(RequestType requestType) => new(requestType.Id, requestType.Name, requestType.Description, requestType.IsActive, requestType.CreatedAtUtc);
}
