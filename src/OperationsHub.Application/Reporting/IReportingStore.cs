namespace OperationsHub.Application.Reporting;

public interface IReportingStore
{
    public Task<IReadOnlyList<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync(CancellationToken cancellationToken);
}
