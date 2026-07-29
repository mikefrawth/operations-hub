using OperationsHub.Domain.Enums;

namespace OperationsHub.Application.Requests;

public sealed record RequestActor(string UserId, RequestActorRole Role);

public enum RequestActorRole { Requester, Technician, Manager, Administrator }

public sealed record CreateServiceRequestCommand(string? Title, string? Description, Guid RequestTypeId, ServiceRequestPriority Priority, Guid? DepartmentId);
public sealed record UpdateServiceRequestCommand(string? Title, string? Description, Guid RequestTypeId, ServiceRequestPriority Priority, Guid? DepartmentId);
public sealed record AssignServiceRequestCommand(string? AssigneeId);
public sealed record ChangeServiceRequestStatusCommand(ServiceRequestStatus Status);
public sealed record AddRequestCommentCommand(string? Body);
public sealed record ServiceRequestSearchQuery(int Page = 1, int PageSize = 20, string? Search = null, ServiceRequestStatus? Status = null, ServiceRequestPriority? Priority = null);
public sealed record ServiceRequestListItemDto(Guid Id, string RequestNumber, string Title, ServiceRequestStatus Status, ServiceRequestPriority Priority, string RequesterId, string? AssigneeId, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record RequestAssignmentDto(string AssigneeId, string AssignedById, DateTimeOffset AssignedAtUtc);
public sealed record RequestCommentDto(string AuthorId, string Body, DateTimeOffset CreatedAtUtc);
public sealed record RequestStatusHistoryDto(ServiceRequestStatus Status, string ChangedById, DateTimeOffset ChangedAtUtc);
public sealed record ServiceRequestDetailDto(Guid Id, string RequestNumber, string Title, string Description, Guid RequestTypeId, Guid? DepartmentId, ServiceRequestStatus Status, ServiceRequestPriority Priority, string RequesterId, string? AssigneeId, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, IReadOnlyList<RequestAssignmentDto> Assignments, IReadOnlyList<RequestCommentDto> Comments, IReadOnlyList<RequestStatusHistoryDto> StatusHistory);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public enum RequestOperationStatus { Success, ValidationFailed, NotFound, Forbidden }
public sealed record RequestOperationResult<T>(RequestOperationStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Status == RequestOperationStatus.Success;
}
