using OperationsHub.Domain.Entities;

namespace OperationsHub.Application.Requests;

public interface IServiceRequestStore
{
    public Task<bool> RequestTypeIsActiveAsync(Guid id, CancellationToken cancellationToken);
    public Task<bool> DepartmentIsActiveAsync(Guid id, CancellationToken cancellationToken);
    public Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken);
    public Task<ServiceRequest?> FindAsync(Guid id, CancellationToken cancellationToken);
    public Task<IReadOnlyList<RequestAssignment>> GetAssignmentsAsync(Guid requestId, CancellationToken cancellationToken);
    public Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken);
    public Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken);
    public Task<PagedResult<ServiceRequest>> SearchAsync(ServiceRequestSearchQuery query, string? requesterId, string? assigneeId, CancellationToken cancellationToken);
    public void Add(ServiceRequest request);
    public void Add(RequestAssignment assignment);
    public void Add(RequestComment comment);
    public void Add(RequestStatusHistory history);
    public void Add(AuditEvent auditEvent);
    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
