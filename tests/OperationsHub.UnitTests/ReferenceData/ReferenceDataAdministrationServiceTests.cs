using OperationsHub.Application.ReferenceData;
using OperationsHub.Domain.Entities;

namespace OperationsHub.UnitTests.ReferenceData;

public sealed class ReferenceDataAdministrationServiceTests
{
    [Fact]
    public async Task CreateDepartmentAsyncTrimsNameAndPersistsActiveDepartment()
    {
        var store = new InMemoryReferenceDataStore();
        var service = new ReferenceDataAdministrationService(store, TimeProvider.System);

        var result = await service.CreateDepartmentAsync(new CreateDepartmentCommand("  Finance  "), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Finance", result.Value!.Name);
        Assert.True(result.Value.IsActive);
        Assert.Single(store.Departments);
    }

    [Fact]
    public async Task CreateDepartmentAsyncRejectsDuplicateName()
    {
        var store = new InMemoryReferenceDataStore();
        store.Departments.Add(new Department(Guid.NewGuid(), "Finance", DateTimeOffset.UtcNow));
        var service = new ReferenceDataAdministrationService(store, TimeProvider.System);

        var result = await service.CreateDepartmentAsync(new CreateDepartmentCommand("Finance"), CancellationToken.None);

        Assert.Equal(ReferenceDataOperationStatus.Conflict, result.Status);
        Assert.Equal("A department with this name already exists.", result.Errors["name"].Single());
        Assert.Single(store.Departments);
    }

    [Fact]
    public async Task UpdateRequestTypeAsyncRejectsDescriptionLongerThanMaximum()
    {
        var store = new InMemoryReferenceDataStore();
        var requestType = new RequestType(Guid.NewGuid(), "Access", "Original", DateTimeOffset.UtcNow);
        store.RequestTypes.Add(requestType);
        var service = new ReferenceDataAdministrationService(store, TimeProvider.System);

        var result = await service.UpdateRequestTypeAsync(requestType.Id, new UpdateRequestTypeCommand("Access", new string('x', 501)), CancellationToken.None);

        Assert.Equal(ReferenceDataOperationStatus.ValidationFailed, result.Status);
        Assert.Equal("Description cannot exceed 500 characters.", result.Errors["description"].Single());
        Assert.Equal("Original", requestType.Description);
    }

    [Fact]
    public async Task DeactivateDepartmentAsyncRetainsTheDepartmentAndMarksItInactive()
    {
        var store = new InMemoryReferenceDataStore();
        var department = new Department(Guid.NewGuid(), "Facilities", DateTimeOffset.UtcNow);
        store.Departments.Add(department);
        var service = new ReferenceDataAdministrationService(store, TimeProvider.System);

        var result = await service.DeactivateDepartmentAsync(department.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(department.IsActive);
        Assert.Single(store.Departments);
    }

    [Fact]
    public void DepartmentRenameRejectsBlankName()
    {
        var department = new Department(Guid.NewGuid(), "Facilities", DateTimeOffset.UtcNow);

        var exception = Assert.Throws<ArgumentException>(() => department.Rename(" "));

        Assert.Equal("name", exception.ParamName);
        Assert.Equal("Facilities", department.Name);
    }

    private sealed class InMemoryReferenceDataStore : IReferenceDataStore
    {
        public List<Department> Departments { get; } = [];

        public List<RequestType> RequestTypes { get; } = [];

        public Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Department>>(Departments.Where(department => !activeOnly || department.IsActive).OrderBy(department => department.Name).ToList());

        public Task<IReadOnlyList<RequestType>> GetRequestTypesAsync(bool activeOnly, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequestType>>(RequestTypes.Where(requestType => !activeOnly || requestType.IsActive).OrderBy(requestType => requestType.Name).ToList());

        public Task<Department?> FindDepartmentAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Departments.SingleOrDefault(department => department.Id == id));

        public Task<RequestType?> FindRequestTypeAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(RequestTypes.SingleOrDefault(requestType => requestType.Id == id));

        public Task<bool> DepartmentNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(Departments.Any(department => department.Name == name && department.Id != excludedId));

        public Task<bool> RequestTypeNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(RequestTypes.Any(requestType => requestType.Name == name && requestType.Id != excludedId));

        public void Add(Department department) => Departments.Add(department);

        public void Add(RequestType requestType) => RequestTypes.Add(requestType);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
