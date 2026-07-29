namespace OperationsHub.Application.Requests;

public interface IServiceRequestWorkflowService
{
    public Task<RequestOperationResult<ServiceRequestDetailDto>> CreateAsync(RequestActor actor, CreateServiceRequestCommand command, CancellationToken cancellationToken);
    public Task<RequestOperationResult<ServiceRequestDetailDto>> GetAsync(RequestActor actor, Guid id, CancellationToken cancellationToken);
    public Task<PagedResult<ServiceRequestListItemDto>> SearchAsync(RequestActor actor, ServiceRequestSearchQuery query, CancellationToken cancellationToken);
    public Task<RequestOperationResult<ServiceRequestDetailDto>> UpdateAsync(RequestActor actor, Guid id, UpdateServiceRequestCommand command, CancellationToken cancellationToken);
    public Task<RequestOperationResult<ServiceRequestDetailDto>> AssignAsync(RequestActor actor, Guid id, AssignServiceRequestCommand command, CancellationToken cancellationToken);
    public Task<RequestOperationResult<ServiceRequestDetailDto>> ChangeStatusAsync(RequestActor actor, Guid id, ChangeServiceRequestStatusCommand command, CancellationToken cancellationToken);
    public Task<RequestOperationResult<ServiceRequestDetailDto>> AddCommentAsync(RequestActor actor, Guid id, AddRequestCommentCommand command, CancellationToken cancellationToken);
}
