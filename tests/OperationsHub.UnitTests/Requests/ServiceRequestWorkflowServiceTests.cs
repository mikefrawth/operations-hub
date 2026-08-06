using OperationsHub.Application.Requests;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.UnitTests.Requests;

public sealed class ServiceRequestWorkflowServiceTests
{
    [Fact]
    public async Task AssignAsyncRejectsAnIdentityUserWhoIsNotAnActiveTechnician()
    {
        var request = CreateRequest();
        var store = new WorkflowStore(request) { ActiveTechnicianExists = false };
        var service = new ServiceRequestWorkflowService(store, TimeProvider.System);

        var result = await service.AssignAsync(
            new RequestActor("manager", RequestActorRole.Manager),
            request.Id,
            new AssignServiceRequestCommand("requester", request.Version),
            CancellationToken.None);

        Assert.Equal(RequestOperationStatus.ValidationFailed, result.Status);
        Assert.Contains("assigneeId", result.Errors.Keys);
        Assert.Equal(0, store.ProcedureCalls);
    }

    [Fact]
    public async Task SearchAsyncRejectsAnUnknownActorRoleInsteadOfReturningAllRequests()
    {
        var store = new WorkflowStore(CreateRequest());
        var service = new ServiceRequestWorkflowService(store, TimeProvider.System);
        var actor = new RequestActor("user", (RequestActorRole)999);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SearchAsync(actor, new ServiceRequestSearchQuery(), CancellationToken.None));
    }

    private static ServiceRequest CreateRequest() => new(
        Guid.NewGuid(),
        "SR-TEST-WORKFLOW",
        "Workflow test",
        "Verify server-side assignment validation.",
        "requester",
        Guid.NewGuid(),
        ServiceRequestPriority.Normal,
        DateTimeOffset.UtcNow);

    private sealed class WorkflowStore : IServiceRequestStore
    {
        private readonly ServiceRequest request;

        public WorkflowStore(ServiceRequest request) => this.request = request;

        public bool ActiveTechnicianExists { get; init; }

        public int ProcedureCalls { get; private set; }

        public Task<bool> RequestTypeIsActiveAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> DepartmentIsActiveAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> ActiveTechnicianExistsAsync(string id, DateTimeOffset asOfUtc, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveTechnicianExists);

        public Task<ServiceRequest?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ServiceRequest?>(id == request.Id ? request : null);

        public Task RefreshAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<RequestAssignment>> GetAssignmentsAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestAssignment>>([]);

        public Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestComment>>([]);

        public Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestStatusHistory>>([]);

        public Task<PagedResult<ServiceRequest>> SearchAsync(ServiceRequestSearchQuery query, string? requesterId, string? assigneeId, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<ServiceRequest>([], query.Page, query.PageSize, 0));

        public Task<IReadOnlyList<OpenRequestSummaryDto>> GetOpenRequestSummariesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OpenRequestSummaryDto>>([]);

        public Task<ProcedureAssignmentResult> AssignUsingProcedureAsync(Guid requestId, string assigneeId, string actorId, uint expectedVersion, DateTimeOffset assignedAtUtc, CancellationToken cancellationToken)
        {
            ProcedureCalls++;
            return Task.FromResult(new ProcedureAssignmentResult(ProcedureAssignmentStatus.Success, expectedVersion + 1));
        }

        public void Add(ServiceRequest serviceRequest) => throw new NotSupportedException();

        public void Add(RequestAssignment assignment) => throw new NotSupportedException();

        public void Add(RequestComment comment) => throw new NotSupportedException();

        public void Add(RequestStatusHistory history) => throw new NotSupportedException();

        public void Add(AuditEvent auditEvent) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
