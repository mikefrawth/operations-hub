using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OperationsHub.Application.Reporting;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;
using OperationsHub.Infrastructure.Persistence;
using OperationsHub.Infrastructure.Reporting;

namespace OperationsHub.IntegrationTests;

public sealed class DepartmentPerformanceReportingMySqlTests
{
    [Fact]
    public async Task DepartmentPerformanceViewCalculatesVolumeResolutionAndSlaMetrics()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:OperationsHub"] = connectionString });
        builder.Services.AddOperationsHubPersistence(builder.Configuration);
        using var host = builder.Build();
        await host.Services.InitializeOperationsHubDevelopmentDatabaseAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        var requestTypeId = await database.RequestTypes.Select(requestType => requestType.Id).FirstAsync(CancellationToken.None);
        var requesterId = await database.Users.Where(user => user.Email == "requester@operationshub.local").Select(user => user.Id).SingleAsync(CancellationToken.None);
        var managerId = await database.Users.Where(user => user.Email == "manager@operationshub.local").Select(user => user.Id).SingleAsync(CancellationToken.None);
        var department = new Department(Guid.NewGuid(), $"Reporting test {Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        var failedResolution = DateTimeOffset.UtcNow.AddHours(-2);
        var metResolution = DateTimeOffset.UtcNow.AddHours(-1);
        var failedRequest = CreateResolvedRequest(requestTypeId, requesterId, department.Id, ServiceRequestPriority.High, failedResolution.AddHours(-10), failedResolution);
        var metRequest = CreateResolvedRequest(requestTypeId, requesterId, department.Id, ServiceRequestPriority.Normal, metResolution.AddHours(-48), metResolution);
        database.Departments.Add(department);
        database.ServiceRequests.AddRange(failedRequest, metRequest);
        database.RequestStatusHistories.AddRange(
            new RequestStatusHistory(Guid.NewGuid(), failedRequest.Id, ServiceRequestStatus.Resolved, managerId, failedResolution),
            new RequestStatusHistory(Guid.NewGuid(), metRequest.Id, ServiceRequestStatus.Resolved, managerId, metResolution));
        await database.SaveChangesAsync(CancellationToken.None);

        try
        {
            var report = await new EntityFrameworkReportingStore(database).GetDepartmentPerformanceAsync(CancellationToken.None);
            var item = Assert.Single(report, row => row.DepartmentId == department.Id);

            Assert.Equal(2, item.TotalRequests);
            Assert.Equal(0, item.OpenRequests);
            Assert.Equal(2, item.CompletedRequests);
            Assert.Equal(29.0m, item.AverageResolutionHours);
            Assert.Equal(2, item.SlaEligibleRequests);
            Assert.Equal(1, item.SlaMetRequests);
            Assert.Equal(50.0m, item.SlaCompliancePercentage);
        }
        finally
        {
            await database.RequestStatusHistories.Where(history => history.ServiceRequestId == failedRequest.Id || history.ServiceRequestId == metRequest.Id).ExecuteDeleteAsync(CancellationToken.None);
            await database.ServiceRequests.Where(request => request.Id == failedRequest.Id || request.Id == metRequest.Id).ExecuteDeleteAsync(CancellationToken.None);
            await database.Departments.Where(item => item.Id == department.Id).ExecuteDeleteAsync(CancellationToken.None);
        }
    }

    private static ServiceRequest CreateResolvedRequest(
        Guid requestTypeId,
        string requesterId,
        Guid departmentId,
        ServiceRequestPriority priority,
        DateTimeOffset createdAtUtc,
        DateTimeOffset resolvedAtUtc)
    {
        var request = new ServiceRequest(
            Guid.NewGuid(),
            $"SR-REPORT-{Guid.NewGuid():N}"[..32],
            "Reporting calculation",
            "A deterministic reporting test request.",
            requesterId,
            requestTypeId,
            priority,
            createdAtUtc);
        request.Update(request.Title, request.Description, request.RequestTypeId, request.Priority, departmentId, createdAtUtc);
        request.ChangeStatus(ServiceRequestStatus.InProgress, createdAtUtc.AddMinutes(1));
        request.ChangeStatus(ServiceRequestStatus.Resolved, resolvedAtUtc);
        return request;
    }
}
