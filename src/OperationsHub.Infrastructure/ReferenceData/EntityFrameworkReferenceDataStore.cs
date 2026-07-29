using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.Infrastructure.ReferenceData;

public sealed class EntityFrameworkReferenceDataStore : IReferenceDataStore
{
    private readonly OperationsHubDbContext database;

    public EntityFrameworkReferenceDataStore(OperationsHubDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = database.Departments.AsNoTracking().OrderBy(department => department.Name);
        if (activeOnly)
        {
            query = query.Where(department => department.IsActive).OrderBy(department => department.Name);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequestType>> GetRequestTypesAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = database.RequestTypes.AsNoTracking().OrderBy(requestType => requestType.Name);
        if (activeOnly)
        {
            query = query.Where(requestType => requestType.IsActive).OrderBy(requestType => requestType.Name);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Department?> FindDepartmentAsync(Guid id, CancellationToken cancellationToken) =>
        database.Departments.SingleOrDefaultAsync(department => department.Id == id, cancellationToken);

    public Task<RequestType?> FindRequestTypeAsync(Guid id, CancellationToken cancellationToken) =>
        database.RequestTypes.SingleOrDefaultAsync(requestType => requestType.Id == id, cancellationToken);

    public Task<bool> DepartmentNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) =>
        database.Departments.AnyAsync(department => department.Name == name && (!excludedId.HasValue || department.Id != excludedId.Value), cancellationToken);

    public Task<bool> RequestTypeNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) =>
        database.RequestTypes.AnyAsync(requestType => requestType.Name == name && (!excludedId.HasValue || requestType.Id != excludedId.Value), cancellationToken);

    public void Add(Department department) => database.Departments.Add(department);

    public void Add(RequestType requestType) => database.RequestTypes.Add(requestType);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
