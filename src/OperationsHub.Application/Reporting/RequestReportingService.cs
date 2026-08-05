using OperationsHub.Application.Requests;

namespace OperationsHub.Application.Reporting;

public sealed class RequestReportingService : IRequestReportingService
{
    private readonly IReportingStore store;

    public RequestReportingService(IReportingStore store) => this.store = store;

    public async Task<RequestOperationResult<IReadOnlyList<DepartmentPerformanceDto>>> GetDepartmentPerformanceAsync(
        RequestActor actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role is not (RequestActorRole.Manager or RequestActorRole.Administrator))
        {
            return new RequestOperationResult<IReadOnlyList<DepartmentPerformanceDto>>(
                RequestOperationStatus.Forbidden,
                default,
                new Dictionary<string, string[]>());
        }

        return new RequestOperationResult<IReadOnlyList<DepartmentPerformanceDto>>(
            RequestOperationStatus.Success,
            await store.GetDepartmentPerformanceAsync(cancellationToken),
            new Dictionary<string, string[]>());
    }
}
