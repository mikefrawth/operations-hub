using OperationsHub.Application.Requests;

namespace OperationsHub.Application.Reporting;

public interface IRequestReportingService
{
    public Task<RequestOperationResult<IReadOnlyList<DepartmentPerformanceDto>>> GetDepartmentPerformanceAsync(RequestActor actor, CancellationToken cancellationToken);
}
