using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Enums;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Infrastructure.Persistence;
using OperationsHub.Infrastructure.Requests;

namespace OperationsHub.IntegrationTests;

public sealed class DevelopmentPortfolioScenarioMySqlTests
{
    private static readonly Guid PortfolioRequestId = Guid.Parse("40000000-0000-4000-8000-000000000001");

    private const string RequesterId = "10000000-0000-4000-8000-000000000001";
    private const string TechnicianId = "10000000-0000-4000-8000-000000000002";
    private const string ManagerId = "10000000-0000-4000-8000-000000000003";
    private const string AdministratorId = "10000000-0000-4000-8000-000000000004";

    [Fact]
    public async Task ScenarioPopulatesEachPermittedRoleViewAndRequestHistory()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:OperationsHub"] = connectionString,
            });
        builder.Services.AddOperationsHubPersistence(builder.Configuration);

        using var host = builder.Build();
        await host.Services.InitializeOperationsHubDevelopmentDatabaseAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        var workflow = new ServiceRequestWorkflowService(
            new EntityFrameworkServiceRequestStore(database),
            TimeProvider.System);
        var userDirectory = new EntityFrameworkUserDisplayDirectory(database);
        var query = new ServiceRequestSearchQuery(Search: "SR-000001");

        var requesterResults = await workflow.SearchAsync(
            new RequestActor(RequesterId, RequestActorRole.Requester),
            query,
            CancellationToken.None);
        var technicianResults = await workflow.SearchAsync(
            new RequestActor(TechnicianId, RequestActorRole.Technician),
            query,
            CancellationToken.None);
        var managerResults = await workflow.SearchAsync(
            new RequestActor(ManagerId, RequestActorRole.Manager),
            query,
            CancellationToken.None);
        var administratorResults = await workflow.SearchAsync(
            new RequestActor(AdministratorId, RequestActorRole.Administrator),
            query,
            CancellationToken.None);
        var technicianDetail = await workflow.GetAsync(
            new RequestActor(TechnicianId, RequestActorRole.Technician),
            PortfolioRequestId,
            CancellationToken.None);
        var openSummary = await workflow.GetOpenRequestSummariesAsync(
            new RequestActor(ManagerId, RequestActorRole.Manager),
            new OpenRequestSummaryQuery(),
            CancellationToken.None);
        var auditEvents = await database.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.ServiceRequestId == PortfolioRequestId)
            .Select(auditEvent => auditEvent.EventType)
            .ToListAsync(CancellationToken.None);
        var participants = await userDirectory.GetByIdsAsync(
            [RequesterId, TechnicianId, ManagerId, AdministratorId],
            CancellationToken.None);

        Assert.Equal(PortfolioRequestId, Assert.Single(requesterResults.Items).Id);
        Assert.Equal(PortfolioRequestId, Assert.Single(technicianResults.Items).Id);
        Assert.Equal(PortfolioRequestId, Assert.Single(managerResults.Items).Id);
        Assert.Equal(PortfolioRequestId, Assert.Single(administratorResults.Items).Id);
        Assert.True(technicianDetail.Succeeded);
        Assert.Equal(ServiceRequestStatus.InProgress, technicianDetail.Value!.Status);
        Assert.Equal(TechnicianId, technicianDetail.Value.AssigneeId);
        Assert.Single(technicianDetail.Value.Assignments);
        Assert.Equal(2, technicianDetail.Value.StatusHistory.Count);
        Assert.Equal(2, technicianDetail.Value.Comments.Count);
        var summary = Assert.Single(openSummary.Value!.Items, item => item.Id == PortfolioRequestId);
        Assert.Equal(ServiceRequestStatus.InProgress, summary.Status);
        Assert.Equal(TechnicianId, summary.AssigneeId);
        Assert.Contains("request-created", auditEvents);
        Assert.Contains("request-assigned", auditEvents);
        Assert.Contains("status-changed", auditEvents);
        Assert.Contains("comment-added", auditEvents);
        Assert.Contains(participants, user => user.Id == RequesterId && user.DisplayName == "Demo Requester");
        Assert.Contains(participants, user => user.Id == TechnicianId && user.DisplayName == "Demo Technician");
        Assert.Contains(participants, user => user.Id == ManagerId && user.DisplayName == "Demo Manager");
        Assert.Contains(participants, user => user.Id == AdministratorId && user.DisplayName == "Demo Administrator");
    }
}
