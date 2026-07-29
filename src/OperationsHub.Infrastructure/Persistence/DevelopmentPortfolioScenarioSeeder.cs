using Microsoft.EntityFrameworkCore;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.Infrastructure.Persistence;

internal static class DevelopmentPortfolioScenarioSeeder
{
    private static readonly Guid FacilitiesRequestId = Guid.Parse("40000000-0000-4000-8000-000000000001");
    private static readonly Guid FacilitiesDepartmentId = Guid.Parse("20000000-0000-4000-8000-000000000002");
    private static readonly Guid FacilitiesRequestTypeId = Guid.Parse("30000000-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    internal static async Task SeedAsync(OperationsHubDbContext database, CancellationToken cancellationToken)
    {
        if (await database.ServiceRequests.AnyAsync(request => request.Id == FacilitiesRequestId, cancellationToken))
        {
            return;
        }

        var request = new ServiceRequest(
            FacilitiesRequestId,
            "SR-000001",
            "Replace the conference room projector lamp",
            "The projector lamp has reached end of life and the room is unavailable for presentations.",
            "10000000-0000-4000-8000-000000000001",
            FacilitiesRequestTypeId,
            ServiceRequestPriority.Normal,
            CreatedAtUtc);
        request.Update(request.Title, request.Description, FacilitiesRequestTypeId, request.Priority, FacilitiesDepartmentId, CreatedAtUtc);

        database.ServiceRequests.Add(request);
        await database.SaveChangesAsync(cancellationToken);
    }
}
