using OperationsHub.Application.Reporting;
using OperationsHub.Application.Requests;

namespace OperationsHub.UnitTests.Reporting;

public sealed class RequestReportingServiceTests
{
    [Fact]
    public async Task ManagerCanReadDepartmentPerformance()
    {
        var expected = new[] { new DepartmentPerformanceDto(Guid.NewGuid(), "Operations", 4, 1, 3, 12.5m, 3, 2) };
        var service = new RequestReportingService(new ReportingStore(expected));

        var result = await service.GetDepartmentPerformanceAsync(
            new RequestActor("manager", RequestActorRole.Manager),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Same(expected, result.Value);
        Assert.Equal(66.7m, result.Value![0].SlaCompliancePercentage);
    }

    [Fact]
    public async Task RequesterCannotReadDepartmentPerformance()
    {
        var service = new RequestReportingService(new ReportingStore([]));

        var result = await service.GetDepartmentPerformanceAsync(
            new RequestActor("requester", RequestActorRole.Requester),
            CancellationToken.None);

        Assert.Equal(RequestOperationStatus.Forbidden, result.Status);
        Assert.Null(result.Value);
    }

    private sealed class ReportingStore : IReportingStore
    {
        private readonly IReadOnlyList<DepartmentPerformanceDto> result;

        public ReportingStore(IReadOnlyList<DepartmentPerformanceDto> result) => this.result = result;

        public Task<IReadOnlyList<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync(CancellationToken cancellationToken) => Task.FromResult(result);
    }
}
