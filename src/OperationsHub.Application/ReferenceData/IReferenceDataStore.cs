using OperationsHub.Domain.Entities;

namespace OperationsHub.Application.ReferenceData;

public interface IReferenceDataStore
{
    public Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly, CancellationToken cancellationToken);

    public Task<IReadOnlyList<RequestType>> GetRequestTypesAsync(bool activeOnly, CancellationToken cancellationToken);

    public Task<Department?> FindDepartmentAsync(Guid id, CancellationToken cancellationToken);

    public Task<RequestType?> FindRequestTypeAsync(Guid id, CancellationToken cancellationToken);

    public Task<bool> DepartmentNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken);

    public Task<bool> RequestTypeNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken);

    public void Add(Department department);

    public void Add(RequestType requestType);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
