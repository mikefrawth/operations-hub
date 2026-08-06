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
        var store = new WorkflowStore(request) { AssignmentStatus = ProcedureAssignmentStatus.InvalidAssignee };
        var service = new ServiceRequestWorkflowService(store, TimeProvider.System);

        var result = await service.AssignAsync(
            new RequestActor("manager", RequestActorRole.Manager),
            request.Id,
            new AssignServiceRequestCommand("requester", request.Version),
            CancellationToken.None);

        Assert.Equal(RequestOperationStatus.ValidationFailed, result.Status);
        Assert.Contains("assigneeId", result.Errors.Keys);
        Assert.Equal(1, store.ProcedureCalls);
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

    [Fact]
    public async Task SearchAsyncBoundsPageSizeOffsetAndSearchLength()
    {
        var store = new WorkflowStore(CreateRequest());
        var service = new ServiceRequestWorkflowService(store, TimeProvider.System);

        await service.SearchAsync(
            new RequestActor("manager", RequestActorRole.Manager),
            new ServiceRequestSearchQuery(int.MaxValue, int.MaxValue, $"  {new string('a', 250)}  "),
            CancellationToken.None);

        Assert.Equal(ServiceRequestSearchQuery.MaximumPage, store.LastSearchQuery!.Page);
        Assert.Equal(ServiceRequestSearchQuery.MaximumPageSize, store.LastSearchQuery.PageSize);
        Assert.Equal(ServiceRequestSearchQuery.MaximumSearchLength, store.LastSearchQuery.Search!.Length);
    }

    [Fact]
    public async Task OpenSummaryAsyncBoundsRequestedPage()
    {
        var store = new WorkflowStore(CreateRequest());
        var service = new ServiceRequestWorkflowService(store, TimeProvider.System);

        var result = await service.GetOpenRequestSummariesAsync(
            new RequestActor("manager", RequestActorRole.Manager),
            new OpenRequestSummaryQuery(int.MaxValue, int.MaxValue),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal((ServiceRequestSearchQuery.MaximumPage, ServiceRequestSearchQuery.MaximumPageSize), store.LastOpenSummaryPage);
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

        public ProcedureAssignmentStatus AssignmentStatus { get; init; } = ProcedureAssignmentStatus.Success;

        public int ProcedureCalls { get; private set; }

        public ServiceRequestSearchQuery? LastSearchQuery { get; private set; }

        public (int Page, int PageSize) LastOpenSummaryPage { get; private set; }

        public Task<bool> RequestTypeIsActiveAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> DepartmentIsActiveAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<ServiceRequest?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ServiceRequest?>(id == request.Id ? request : null);

        public Task RefreshAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<RequestAssignment>> GetAssignmentsAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestAssignment>>([]);

        public Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestComment>>([]);

        public Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestStatusHistory>>([]);

        public Task<PagedResult<ServiceRequest>> SearchAsync(ServiceRequestSearchQuery query, string? requesterId, string? assigneeId, CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            return Task.FromResult(new PagedResult<ServiceRequest>([], query.Page, query.PageSize, 0));
        }

        public Task<PagedResult<OpenRequestSummaryDto>> GetOpenRequestSummariesAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            LastOpenSummaryPage = (page, pageSize);
            return Task.FromResult(new PagedResult<OpenRequestSummaryDto>([], page, pageSize, 0));
        }

        public Task<ProcedureAssignmentResult> AssignUsingProcedureAsync(Guid requestId, string assigneeId, string actorId, uint expectedVersion, DateTimeOffset assignedAtUtc, CancellationToken cancellationToken)
        {
            ProcedureCalls++;
            return Task.FromResult(new ProcedureAssignmentResult(AssignmentStatus, expectedVersion + 1));
        }

        public void Add(ServiceRequest serviceRequest) => throw new NotSupportedException();

        public void Add(RequestAssignment assignment) => throw new NotSupportedException();

        public void Add(RequestComment comment) => throw new NotSupportedException();

        public void Add(RequestStatusHistory history) => throw new NotSupportedException();

        public void Add(AuditEvent auditEvent) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
