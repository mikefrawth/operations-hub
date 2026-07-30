using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;
using OperationsHub.Infrastructure.Persistence;
using OperationsHub.Infrastructure.Requests;

namespace OperationsHub.IntegrationTests;

public sealed class RequestReportingMySqlTests
{
    [Fact]
    public async Task AssignmentProcedureCommitsAllWritesAndRollsBackStaleVersionAttempt()
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
        var technicianId = await database.Users.Where(user => user.Email == "technician@operationshub.local").Select(user => user.Id).SingleAsync(CancellationToken.None);
        var managerId = await database.Users.Where(user => user.Email == "manager@operationshub.local").Select(user => user.Id).SingleAsync(CancellationToken.None);
        var request = new ServiceRequest(Guid.NewGuid(), $"SR-TEST-{Guid.NewGuid():N}"[..32], "Procedure assignment", "Verify transactional assignment behavior.", requesterId, requestTypeId, ServiceRequestPriority.Normal, DateTimeOffset.UtcNow);
        database.ServiceRequests.Add(request);
        await database.SaveChangesAsync(CancellationToken.None);

        try
        {
            var store = new EntityFrameworkServiceRequestStore(database);
            var workflow = new ServiceRequestWorkflowService(store, TimeProvider.System);
            var actor = new RequestActor(managerId, RequestActorRole.Manager);

            var assigned = await workflow.AssignAsync(actor, request.Id, new AssignServiceRequestCommand(technicianId, 0), CancellationToken.None);
            var assignmentCount = await database.RequestAssignments.CountAsync(assignment => assignment.ServiceRequestId == request.Id, CancellationToken.None);
            var auditCount = await database.AuditEvents.CountAsync(audit => audit.ServiceRequestId == request.Id && audit.EventType == "request-assigned", CancellationToken.None);
            var staleAttempt = await store.AssignUsingProcedureAsync(request.Id, technicianId, managerId, 0, DateTimeOffset.UtcNow, CancellationToken.None);
            var conflictResponse = await workflow.AssignAsync(actor, request.Id, new AssignServiceRequestCommand(technicianId, 0), CancellationToken.None);
            var summaries = await store.GetOpenRequestSummariesAsync(CancellationToken.None);

            Assert.True(assigned.Succeeded);
            Assert.Equal((uint)1, assigned.Value!.Version);
            Assert.Equal(1, assignmentCount);
            Assert.Equal(1, auditCount);
            Assert.Equal(ProcedureAssignmentStatus.Conflict, staleAttempt.Status);
            Assert.Equal(RequestOperationStatus.Conflict, conflictResponse.Status);
            Assert.Equal(1, await database.RequestAssignments.CountAsync(assignment => assignment.ServiceRequestId == request.Id, CancellationToken.None));
            Assert.Equal(1, await database.AuditEvents.CountAsync(audit => audit.ServiceRequestId == request.Id && audit.EventType == "request-assigned", CancellationToken.None));
            var summary = Assert.Single(summaries, summary => summary.Id == request.Id);
            Assert.Equal(technicianId, summary.AssigneeId);
            Assert.True(summary.Age >= TimeSpan.Zero);
        }
        finally
        {
            await database.AuditEvents.Where(audit => audit.ServiceRequestId == request.Id).ExecuteDeleteAsync(CancellationToken.None);
            await database.RequestAssignments.Where(assignment => assignment.ServiceRequestId == request.Id).ExecuteDeleteAsync(CancellationToken.None);
            await database.ServiceRequests.Where(serviceRequest => serviceRequest.Id == request.Id).ExecuteDeleteAsync(CancellationToken.None);
        }
    }
}
