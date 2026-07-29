using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.Infrastructure.Requests;

public sealed class EntityFrameworkServiceRequestStore : IServiceRequestStore
{
    private readonly OperationsHubDbContext database;

    public EntityFrameworkServiceRequestStore(OperationsHubDbContext database) => this.database = database;

    public Task<bool> RequestTypeIsActiveAsync(Guid id, CancellationToken cancellationToken) => database.RequestTypes.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    public Task<bool> DepartmentIsActiveAsync(Guid id, CancellationToken cancellationToken) => database.Departments.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    public Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken) => database.Users.AnyAsync(x => x.Id == id, cancellationToken);
    public Task<ServiceRequest?> FindAsync(Guid id, CancellationToken cancellationToken) => database.ServiceRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<RequestAssignment>> GetAssignmentsAsync(Guid requestId, CancellationToken cancellationToken) => await database.RequestAssignments.AsNoTracking().Where(x => x.ServiceRequestId == requestId).OrderBy(x => x.AssignedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken) => await database.RequestComments.AsNoTracking().Where(x => x.ServiceRequestId == requestId).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken) => await database.RequestStatusHistories.AsNoTracking().Where(x => x.ServiceRequestId == requestId).OrderBy(x => x.ChangedAtUtc).ToListAsync(cancellationToken);

    public async Task<PagedResult<ServiceRequest>> SearchAsync(ServiceRequestSearchQuery query, string? requesterId, string? assigneeId, CancellationToken cancellationToken)
    {
        var requests = database.ServiceRequests.AsNoTracking().AsQueryable();
        if (requesterId is not null) requests = requests.Where(x => x.RequesterId == requesterId);
        if (assigneeId is not null) requests = requests.Where(x => x.AssigneeId == assigneeId);
        if (query.Status.HasValue) requests = requests.Where(x => x.Status == query.Status.Value);
        if (query.Priority.HasValue) requests = requests.Where(x => x.Priority == query.Priority.Value);
        if (query.Search is not null) requests = requests.Where(x => x.RequestNumber.Contains(query.Search) || x.Title.Contains(query.Search));
        var total = await requests.CountAsync(cancellationToken);
        var items = await requests.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.RequestNumber).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<ServiceRequest>(items, query.Page, query.PageSize, total);
    }

    public void Add(ServiceRequest request) => database.ServiceRequests.Add(request);
    public void Add(RequestAssignment assignment) => database.RequestAssignments.Add(assignment);
    public void Add(RequestComment comment) => database.RequestComments.Add(comment);
    public void Add(RequestStatusHistory history) => database.RequestStatusHistories.Add(history);
    public void Add(AuditEvent auditEvent) => database.AuditEvents.Add(auditEvent);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
