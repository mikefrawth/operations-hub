using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Infrastructure.Persistence;
using OperationsHub.Infrastructure.ReferenceData;

namespace OperationsHub.IntegrationTests;

public sealed class ReferenceDataMySqlTests
{
    [Fact]
    public async Task ReferenceDataServiceCreatesListsAndDeactivatesRecordsAgainstMySql()
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";
        var options = new DbContextOptionsBuilder<OperationsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        await using var context = new OperationsHubDbContext(options);
        await context.Database.MigrateAsync(CancellationToken.None);
        await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);
        var service = new ReferenceDataAdministrationService(new EntityFrameworkReferenceDataStore(context), TimeProvider.System);
        var suffix = Guid.NewGuid().ToString("N");

        var department = await service.CreateDepartmentAsync(new CreateDepartmentCommand($"Test Department {suffix}"), CancellationToken.None);
        var requestType = await service.CreateRequestTypeAsync(new CreateRequestTypeCommand($"Test Request Type {suffix}", "Integration test record."), CancellationToken.None);
        var deactivated = await service.DeactivateRequestTypeAsync(requestType.Value!.Id, CancellationToken.None);
        var activeRequestTypes = await service.GetRequestTypesAsync(true, CancellationToken.None);

        Assert.True(department.Succeeded);
        Assert.True(requestType.Succeeded);
        Assert.True(deactivated.Succeeded);
        Assert.DoesNotContain(activeRequestTypes, item => item.Id == requestType.Value.Id);
        Assert.False(deactivated.Value!.IsActive);

        await transaction.RollbackAsync(CancellationToken.None);
    }
}
