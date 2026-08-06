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
        var page = Math.Clamp(query.Page, 1, ServiceRequestSearchQuery.MaximumPage);
        var pageSize = Math.Clamp(query.PageSize, 1, ServiceRequestSearchQuery.MaximumPageSize);
        var search = query.Search?.Trim();
        if (search?.Length > ServiceRequestSearchQuery.MaximumSearchLength)
        {
            search = search[..ServiceRequestSearchQuery.MaximumSearchLength];
        }

        if (string.IsNullOrEmpty(search)) search = null;
        var normalized = query with { Page = page, PageSize = pageSize, Search = search };
        var scope = actor.Role switch
        {
            RequestActorRole.Requester => (RequesterId: actor.UserId, AssigneeId: (string?)null),
            RequestActorRole.Technician => (RequesterId: (string?)null, AssigneeId: actor.UserId),
            RequestActorRole.Manager or RequestActorRole.Administrator => (RequesterId: (string?)null, AssigneeId: (string?)null),
            _ => throw new ArgumentOutOfRangeException(nameof(actor), "The request actor role is invalid."),
        };
        var result = await store.SearchAsync(normalized, scope.RequesterId, scope.AssigneeId, cancellationToken);
        return new PagedResult<ServiceRequestListItemDto>(result.Items.Select(MapList).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<RequestOperationResult<PagedResult<OpenRequestSummaryDto>>> GetOpenRequestSummariesAsync(RequestActor actor, OpenRequestSummaryQuery query, CancellationToken cancellationToken)
    {
        if (actor.Role is not (RequestActorRole.Manager or RequestActorRole.Administrator)) return Forbidden<PagedResult<OpenRequestSummaryDto>>();
        var page = Math.Clamp(query.Page, 1, ServiceRequestSearchQuery.MaximumPage);
        var pageSize = Math.Clamp(query.PageSize, 1, ServiceRequestSearchQuery.MaximumPageSize);
        return Success(await store.GetOpenRequestSummariesAsync(page, pageSize, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> UpdateAsync(RequestActor actor, Guid id, UpdateServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (actor.Role != RequestActorRole.Requester || request.RequesterId != actor.UserId) return Forbidden<ServiceRequestDetailDto>();
        if (request.Version != command.Version) return Conflict<ServiceRequestDetailDto>();
        var validation = await ValidateRequestAsync(command.Title, command.Description, command.RequestTypeId, command.Priority, command.DepartmentId, cancellationToken);
        if (validation is not null) return validation;

        try { request.Update(command.Title!.Trim(), command.Description!.Trim(), command.RequestTypeId, command.Priority, command.DepartmentId, timeProvider.GetUtcNow()); request.AdvanceVersion(); }
        catch (InvalidOperationException exception) { return Validation<ServiceRequestDetailDto>("status", exception.Message); }
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "request-updated", actor.UserId, null, request.UpdatedAtUtc));
        try { await store.SaveChangesAsync(cancellationToken); }
        catch (RequestStoreConcurrencyException) { return Conflict<ServiceRequestDetailDto>(); }
        return Success(await MapAsync(request, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> AssignAsync(RequestActor actor, Guid id, AssignServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (actor.Role is not (RequestActorRole.Manager or RequestActorRole.Administrator)) return Forbidden<ServiceRequestDetailDto>();
        var assigneeId = command.AssigneeId?.Trim();
        if (string.IsNullOrWhiteSpace(assigneeId)) return Validation<ServiceRequestDetailDto>("assigneeId", "Assignee is required.");
        var now = timeProvider.GetUtcNow();
        if (request.Version != command.Version) return Conflict<ServiceRequestDetailDto>();
        var result = await store.AssignUsingProcedureAsync(request.Id, assigneeId, actor.UserId, command.Version, now, cancellationToken);
        if (result.Status == ProcedureAssignmentStatus.NotFound) return NotFound<ServiceRequestDetailDto>();
        if (result.Status == ProcedureAssignmentStatus.Conflict) return Conflict<ServiceRequestDetailDto>();
        if (result.Status == ProcedureAssignmentStatus.Closed) return Validation<ServiceRequestDetailDto>("status", "Closed requests cannot be assigned.");
        if (result.Status == ProcedureAssignmentStatus.InvalidAssignee) return Validation<ServiceRequestDetailDto>("assigneeId", "An active technician is required.");
        await store.RefreshAsync(request, cancellationToken);
        return Success(await MapAsync(request, cancellationToken));
    }

    public async Task<RequestOperationResult<ServiceRequestDetailDto>> ChangeStatusAsync(RequestActor actor, Guid id, ChangeServiceRequestStatusCommand command, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(id, cancellationToken);
        if (request is null) return NotFound<ServiceRequestDetailDto>();
        if (!CanManage(actor, request)) return Forbidden<ServiceRequestDetailDto>();
        if (request.Version != command.Version) return Conflict<ServiceRequestDetailDto>();
        var now = timeProvider.GetUtcNow();
        try { request.ChangeStatus(command.Status, now); request.AdvanceVersion(); }
        catch (InvalidOperationException exception) { return Validation<ServiceRequestDetailDto>("status", exception.Message); }
        store.Add(new RequestStatusHistory(Guid.NewGuid(), request.Id, command.Status, actor.UserId, now));
        store.Add(new AuditEvent(Guid.NewGuid(), request.Id, "status-changed", actor.UserId, command.Status.ToString(), now));
        try { await store.SaveChangesAsync(cancellationToken); }
        catch (RequestStoreConcurrencyException) { return Conflict<ServiceRequestDetailDto>(); }
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

    private static bool CanView(RequestActor actor, ServiceRequest request) => actor.Role switch
    {
        RequestActorRole.Requester => request.RequesterId == actor.UserId,
        RequestActorRole.Technician => request.AssigneeId == actor.UserId,
        RequestActorRole.Manager or RequestActorRole.Administrator => true,
        _ => false,
    };
    private static bool CanManage(RequestActor actor, ServiceRequest request) => actor.Role is RequestActorRole.Manager or RequestActorRole.Administrator || actor.Role == RequestActorRole.Technician && request.AssigneeId == actor.UserId;
    private async Task<ServiceRequestDetailDto> MapAsync(ServiceRequest request, CancellationToken cancellationToken) => new(request.Id, request.RequestNumber, request.Title, request.Description, request.RequestTypeId, request.DepartmentId, request.Status, request.Priority, request.RequesterId, request.AssigneeId, request.CreatedAtUtc, request.UpdatedAtUtc, request.Version, (await store.GetAssignmentsAsync(request.Id, cancellationToken)).Select(x => new RequestAssignmentDto(x.AssigneeId, x.AssignedById, x.AssignedAtUtc)).ToList(), (await store.GetCommentsAsync(request.Id, cancellationToken)).Select(x => new RequestCommentDto(x.AuthorId, x.Body, x.CreatedAtUtc)).ToList(), (await store.GetStatusHistoryAsync(request.Id, cancellationToken)).Select(x => new RequestStatusHistoryDto(x.Status, x.ChangedById, x.ChangedAtUtc)).ToList());
    private static ServiceRequestListItemDto MapList(ServiceRequest x) => new(x.Id, x.RequestNumber, x.Title, x.Status, x.Priority, x.RequesterId, x.AssigneeId, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static RequestOperationResult<T> Success<T>(T value) => new(RequestOperationStatus.Success, value, new Dictionary<string, string[]>());
    private static RequestOperationResult<T> Validation<T>(string field, string message) => new(RequestOperationStatus.ValidationFailed, default, new Dictionary<string, string[]> { [field] = [message] });
    private static RequestOperationResult<T> NotFound<T>() => new(RequestOperationStatus.NotFound, default, new Dictionary<string, string[]>());
    private static RequestOperationResult<T> Forbidden<T>() => new(RequestOperationStatus.Forbidden, default, new Dictionary<string, string[]>());
    private static RequestOperationResult<T> Conflict<T>() => new(RequestOperationStatus.Conflict, default, new Dictionary<string, string[]> { ["version"] = ["The request was changed by another user. Refresh and try again."] });
}
