namespace OperationsHub.Application.ReferenceData;

public sealed record DepartmentDto(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAtUtc);

public sealed record RequestTypeDto(Guid Id, string Name, string? Description, bool IsActive, DateTimeOffset CreatedAtUtc);

public sealed record CreateDepartmentCommand(string? Name);

public sealed record UpdateDepartmentCommand(string? Name);

public sealed record CreateRequestTypeCommand(string? Name, string? Description);

public sealed record UpdateRequestTypeCommand(string? Name, string? Description);

public enum ReferenceDataOperationStatus
{
    Success,
    ValidationFailed,
    Conflict,
    NotFound,
}

public sealed record ReferenceDataOperationResult<T>(ReferenceDataOperationStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Status == ReferenceDataOperationStatus.Success;
}

public static class ReferenceDataOperationResults
{
    public static ReferenceDataOperationResult<T> Success<T>(T value) => new(ReferenceDataOperationStatus.Success, value, new Dictionary<string, string[]>());

    public static ReferenceDataOperationResult<T> Failure<T>(ReferenceDataOperationStatus status, string field, string error) =>
        new(status, default, new Dictionary<string, string[]> { [field] = [error] });
}
