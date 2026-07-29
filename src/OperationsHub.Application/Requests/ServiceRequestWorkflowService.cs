using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.Application.Requests;

public sealed class ServiceRequestWorkflowService : IServiceRequestWorkflowService
{
    private readonly IServiceRequestStore store;
    private readonly TimeProvider timeProvider;

    public ServiceRequestWorkflowService(IServiceRequestStore store, TimeProvider timeProvider)
    {
        this.store = store;
        this.timeProvider = timeProvider;
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> CreateAsync(RequestActor actor, CreateServiceRequestCommand command, CancellationToken cancellationToken)
    {
        if (actor.Role != RequestActorRole.Requester)
        {
            return Forbidden<ServiceRequestDetailDto>();
        }

        var validation = await ValidateRequestAsync(command.Title, command.Description, command.RequestTypeId, command.Priority, command.DepartmentId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var now = timeProvider.GetUtcNow();
        var request = new ServiceRequest(Guid.NewGuid(), $"SR-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..32], command.Title!.Trim(), command.Description!.Trim(), actor.UserId, command.RequestTypeId, command.Priority, now);
        request.Update(request.Title, request.Description, request.RequestTypeId, request.Priority, command.DepartmentId, now);
        store.Add(request);
        store.Add(new RequestStatusHistory(Guid.NewGuid(), request.Id, ServiceRequestStatus.New, actor.UserId, now));
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "request-created", actor.UserId, null, now));
        await store.SaveChangesAsync(cancellationToken);
        return Success(await MapAsync(request, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> GetAsync(RequestActor actor, Guid id, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound<ServiceRequestDetailDto>();
        }

        return CanView(actor, request) ? Success(await MapAsync(request, cancellationToken)) : Forbidden<ServiceRequestDetailDto>();
    }

    public async Task<PagedResult<ServiceRequestListItemDto>> SearchAsync(RequestActor actor, ServiceRequestSearchQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalized = query with { Page = page, PageSize = pageSize, Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() };
        var result = await store.SearchAsync(normalized, actor.Role == RequestActorRole.Requester ? actor.UserId : null, actor.Role == RequestActorRole.Technician ? actor.UserId : null, cancellationToken);
        return new PagedResult<ServiceRequestListItemDto>(result.Items.Select(MapList).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> UpdateAsync(RequestActor actor, Guid id, UpdateServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (actor.Role != RequestActorRole.Requester || request.RequesterId != actor.UserId) return Forbidden<ServiceRequestDetailDto>();
        var validation = await ValidateRequestAsync(command.Title, command.Description, command.RequestTypeId, command.Priority, command.DepartmentId, cancellationToken);
        if (validation is not null) return validation;

        try { request.Update(command.Title!.Trim(), command.Description!.Trim(), command.RequestTypeId, command.Priority, command.DepartmentId, timeProvider.GetUtcNow()); }
        catch (InvalidOperationException exception) { return Validation<ServiceRequestDetailDto>("status", exception.Message); }
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "request-updated", actor.UserId, null, request.UpdatedAtUtc));
        await store.SaveChangesAsync(cancellationToken);
        return Success(await MapAsync(request, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> AssignAsync(RequestActor actor, Guid id, AssignServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (actor.Role is not (RequestActorRole.Manager or RequestActorRole.Administrator)) return Forbidden<ServiceRequestDetailDto>();
        var assigneeId = command.AssigneeId?.Trim();
        if (string.IsNullOrWhiteSpace(assigneeId)) return Validation<ServiceRequestDetailDto>("assigneeId", "Assignee is required.");
        if (!await store.UserExistsAsync(assigneeId, cancellationToken)) return Validation<ServiceRequestDetailDto>("assigneeId", "The assignee does not exist.");
        var now = timeProvider.GetUtcNow();
        try { request.Assign(assigneeId, now); }
        catch (InvalidOperationException exception) { return Validation<ServiceRequestDetailDto>("status", exception.Message); }
        store.Add(new RequestAssignment(Guid.NewGuid(), request.Id, assigneeId, actor.UserId, now));
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "request-assigned", actor.UserId, assigneeId, now));
        await store.SaveChangesAsync(cancellationToken);
        return Success(await MapAsync(request, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> ChangeStatusAsync(RequestActor actor, Guid id, ChangeServiceRequestStatusCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (!CanManage(actor, request)) return Forbidden<ServiceRequestDetailDto>();
        var now = timeProvider.GetUtcNow();
        try { request.ChangeStatus(command.Status, now); }
        catch (InvalidOperationException exception) { return Validation<ServiceRequestDetailDto>("status", exception.Message); }
        store.Add(new RequestStatusHistory(Guid.NewGuid(), request.Id, command.Status, actor.UserId, now));
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "status-changed", actor.UserId, command.Status.ToString(), now));
        await store.SaveChangesAsync(cancellationToken);
        return Success(await MapAsync(request, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> AddCommentAsync(RequestActor actor, Guid id, AddRequestCommentCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (!CanView(actor, request)) return Forbidden<ServiceRequestDetailDto>();
        var body = command.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body)) return Validation<ServiceRequestDetailDto>("body", "Comment body is required.");
        if (body.Length > 4_000) return Validation<ServiceRequestDetailDto>("body", "Comment body cannot exceed 4000 characters.");
        var now = timeProvider.GetUtcNow();
        store.Add(new RequestComment(Guid.NewGuid(), request.Id, actor.UserId, body, now));
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "comment-added", actor.UserId, null, now));
        await store.SaveChangesAsync(cancellationToken);
        return Success(await MapAsync(request, cancellationToken));
    }

    private async Task<RequestOperationResult<ServiceRequestDetailDto>?> ValidateRequestAsync(string? title, string? description, Guid requestTypeId, ServiceRequestPriority priority, Guid? departmentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title)) return Validation<ServiceRequestDetailDto>("title", "Title is required.");
        if (title.Trim().Length > ServiceRequest.TitleMaximumLength) return Validation<ServiceRequestDetailDto>("title", "Title cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(description)) return Validation<ServiceRequestDetailDto>("description", "Description is required.");
        if (description.Trim().Length > ServiceRequest.DescriptionMaximumLength) return Validation<ServiceRequestDetailDto>("description", "Description cannot exceed 4000 characters.");
        if (!Enum.IsDefined(priority)) return Validation<ServiceRequestDetailDto>("priority", "Priority is invalid.");
        if (!await store.RequestTypeIsActiveAsync(requestTypeId, cancellationToken)) return Validation<ServiceRequestDetailDto>("requestTypeId", "An active request type is required.");
        if (departmentId.HasValue && !await store.DepartmentIsActiveAsync(departmentId.Value, cancellationToken)) return Validation<ServiceRequestDetailDto>("departmentId", "Department must be active.");
        return null;
    }

    private static bool CanView(RequestActor actor, ServiceRequest request) => actor.Role switch { RequestActorRole.Requester => request.RequesterId == actor.UserId, RequestActorRole.Technician => request.AssigneeId == actor.UserId, _ => true };
    private static bool CanManage(RequestActor actor, ServiceRequest request) => actor.Role is RequestActorRole.Manager or RequestActorRole.Administrator || actor.Role == RequestActorRole.Technician && request.AssigneeId == actor.UserId;
    private async Task<ServiceRequestDetailDto> MapAsync(ServiceRequest request, CancellationToken cancellationToken) => new(request.Id, request.RequestNumber, request.Title, request.Description, request.RequestTypeId, request.DepartmentId, request.Status, request.Priority, request.RequesterId, request.AssigneeId, request.CreatedAtUtc, request.UpdatedAtUtc, (await store.GetAssignmentsAsync(request.Id, cancellationToken)).Select(x => new RequestAssignmentDto(x.AssigneeId, x.AssignedById, x.AssignedAtUtc)).ToList(), (await store.GetCommentsAsync(request.Id, cancellationToken)).Select(x => new RequestCommentDto(x.AuthorId, x.Body, x.CreatedAtUtc)).ToList(), (await store.GetStatusHistoryAsync(request.Id, cancellationToken)).Select(x => new RequestStatusHistoryDto(x.Status, x.ChangedById, x.ChangedAtUtc)).ToList());
    private static ServiceRequestListItemDto MapList(ServiceRequest x) => new(x.Id, x.RequestNumber, x.Title, x.Status, x.Priority, x.RequesterId, x.AssigneeId, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static RequestOperationResult<T> Success<T>(T value) => new(RequestOperationStatus.Success, value, new Dictionary<string, string[]>());
    private static RequestOperationResult<T> Validation<T>(string field, string message) => new(RequestOperationStatus.ValidationFailed, default, new Dictionary<string, string[]> { [field] = [message] });
    private static RequestOperationResult<T> NotFound<T>() => new(RequestOperationStatus.NotFound, default, new Dictionary<string, string[]>());
    private static RequestOperationResult<T> Forbidden<T>() => new(RequestOperationStatus.Forbidden, default, new Dictionary<string, string[]>());
}
