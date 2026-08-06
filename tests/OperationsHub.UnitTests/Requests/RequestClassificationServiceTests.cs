using OperationsHub.Application.ReferenceData;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.UnitTests.Requests;

public sealed class RequestClassificationServiceTests
{
    [Fact]
    public async Task ClassifyAsyncSuggestsAccessAndHighPriorityFromDeterministicRules()
    {
        var accessType = new RequestType(Guid.NewGuid(), "Access request", "Request access to an internal system or service.", DateTimeOffset.UtcNow);
        var service = new RequestClassificationService(new ClassificationReferenceDataStore(accessType), new UnavailableAdvisor());

        var result = await service.ClassifyAsync(
            new RequestActor("requester", RequestActorRole.Requester),
            new RequestClassificationCommand("Cannot sign in", "I am locked out of the payroll application."),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(accessType.Id, result.Value!.SuggestedRequestTypeId);
        Assert.Equal(ServiceRequestPriority.High, result.Value.SuggestedPriority);
        Assert.Equal("Cannot sign in I am locked out of the payroll application", result.Value.Summary);
        Assert.Equal(RequestClassificationSource.Deterministic, result.Value.Source);
    }

    [Fact]
    public async Task ClassifyAsyncFallsBackToDeterministicRulesWhenOptionalAdvisorFails()
    {
        var facilitiesType = new RequestType(Guid.NewGuid(), "Facilities issue", "Report an issue with a workplace or facility.", DateTimeOffset.UtcNow);
        var service = new RequestClassificationService(new ClassificationReferenceDataStore(facilitiesType), new ThrowingAdvisor());

        var result = await service.ClassifyAsync(
            new RequestActor("requester", RequestActorRole.Requester),
            new RequestClassificationCommand("Office door is broken", "The door to the office will not lock."),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(facilitiesType.Id, result.Value!.SuggestedRequestTypeId);
        Assert.Equal(RequestClassificationSource.Deterministic, result.Value.Source);
    }

    [Fact]
    public async Task ClassifyAsyncRejectsNonRequesterBeforeReadingReferenceData()
    {
        var store = new ClassificationReferenceDataStore();
        var service = new RequestClassificationService(store, new UnavailableAdvisor());

        var result = await service.ClassifyAsync(
            new RequestActor("manager", RequestActorRole.Manager),
            new RequestClassificationCommand("Need access", "Please grant access."),
            CancellationToken.None);

        Assert.Equal(RequestOperationStatus.Forbidden, result.Status);
        Assert.False(store.RequestTypesRead);
    }

    private sealed class UnavailableAdvisor : IOptionalRequestClassificationAdvisor
    {
        public Task<RequestClassificationAdvice?> ClassifyAsync(RequestClassificationContext context, CancellationToken cancellationToken) =>
            Task.FromResult<RequestClassificationAdvice?>(null);
    }

    private sealed class ThrowingAdvisor : IOptionalRequestClassificationAdvisor
    {
        public Task<RequestClassificationAdvice?> ClassifyAsync(RequestClassificationContext context, CancellationToken cancellationToken) =>
            throw new HttpRequestException("The optional provider is unavailable.");
    }

    private sealed class ClassificationReferenceDataStore : IReferenceDataStore
    {
        private readonly List<RequestType> requestTypes;

        public ClassificationReferenceDataStore(params RequestType[] requestTypes)
        {
            this.requestTypes = requestTypes.ToList();
        }

        public bool RequestTypesRead { get; private set; }

        public Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Department>>([]);

        public Task<IReadOnlyList<RequestType>> GetRequestTypesAsync(bool activeOnly, CancellationToken cancellationToken)
        {
            RequestTypesRead = true;
            return Task.FromResult<IReadOnlyList<RequestType>>(requestTypes.Where(requestType => !activeOnly || requestType.IsActive).ToList());
        }

        public Task<Department?> FindDepartmentAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Department?>(null);

        public Task<RequestType?> FindRequestTypeAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(requestTypes.SingleOrDefault(requestType => requestType.Id == id));

        public Task<bool> DepartmentNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> RequestTypeNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) => Task.FromResult(false);

        public void Add(Department department)
        {
        }

        public void Add(RequestType requestType) => requestTypes.Add(requestType);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
