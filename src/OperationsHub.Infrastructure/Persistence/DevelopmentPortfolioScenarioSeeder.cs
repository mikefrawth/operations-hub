using Microsoft.EntityFrameworkCore;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.Infrastructure.Persistence;

internal static class DevelopmentPortfolioScenarioSeeder
{
    private static readonly Guid FacilitiesRequestId = Guid.Parse("40000000-0000-4000-8000-000000000001");
    private static readonly Guid AssignmentId = Guid.Parse("41000000-0000-4000-8000-000000000001");
    private static readonly Guid InitialStatusHistoryId = Guid.Parse("42000000-0000-4000-8000-000000000001");
    private static readonly Guid InProgressStatusHistoryId = Guid.Parse("42000000-0000-4000-8000-000000000002");
    private static readonly Guid RequesterCommentId = Guid.Parse("43000000-0000-4000-8000-000000000001");
    private static readonly Guid TechnicianCommentId = Guid.Parse("43000000-0000-4000-8000-000000000002");
    private static readonly Guid CreatedAuditEventId = Guid.Parse("44000000-0000-4000-8000-000000000001");
    private static readonly Guid AssignedAuditEventId = Guid.Parse("44000000-0000-4000-8000-000000000002");
    private static readonly Guid StatusAuditEventId = Guid.Parse("44000000-0000-4000-8000-000000000003");
    private static readonly Guid CommentAuditEventId = Guid.Parse("44000000-0000-4000-8000-000000000004");
    private static readonly Guid FacilitiesDepartmentId = Guid.Parse("20000000-0000-4000-8000-000000000002");
    private static readonly Guid FacilitiesRequestTypeId = Guid.Parse("30000000-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AssignedAtUtc = CreatedAtUtc.AddMinutes(20);
    private static readonly DateTimeOffset StartedAtUtc = CreatedAtUtc.AddMinutes(35);
    private static readonly DateTimeOffset RequesterCommentedAtUtc = CreatedAtUtc.AddMinutes(50);
    private static readonly DateTimeOffset TechnicianCommentedAtUtc = CreatedAtUtc.AddHours(2);

    private const string RequesterId = "10000000-0000-4000-8000-000000000001";
    private const string TechnicianId = "10000000-0000-4000-8000-000000000002";
    private const string ManagerId = "10000000-0000-4000-8000-000000000003";

    internal static async Task SeedAsync(OperationsHubDbContext database, CancellationToken cancellationToken)
    {
        var request = await database.ServiceRequests.SingleOrDefaultAsync(
            serviceRequest => serviceRequest.Id == FacilitiesRequestId,
            cancellationToken);
        var hasScenarioHistory = await database.RequestAssignments.AnyAsync(
            assignment => assignment.Id == AssignmentId,
            cancellationToken);

        if (request is not null && (hasScenarioHistory || request.Status != ServiceRequestStatus.New || request.AssigneeId is not null))
        {
            return;
        }

        if (request is null)
        {
            request = new ServiceRequest(
                FacilitiesRequestId,
                "SR-000001",
                "Replace the conference room projector lamp",
                "The projector lamp has reached end of life and the room is unavailable for presentations.",
                RequesterId,
                FacilitiesRequestTypeId,
                ServiceRequestPriority.Normal,
                CreatedAtUtc);
            request.Update(request.Title, request.Description, FacilitiesRequestTypeId, request.Priority, FacilitiesDepartmentId, CreatedAtUtc);
            database.ServiceRequests.Add(request);
        }

        request.Assign(TechnicianId, AssignedAtUtc);
        request.AdvanceVersion();
        request.ChangeStatus(ServiceRequestStatus.InProgress, StartedAtUtc);
        request.AdvanceVersion();

        database.RequestAssignments.Add(new RequestAssignment(AssignmentId, request.Id, TechnicianId, ManagerId, AssignedAtUtc));
        database.RequestStatusHistories.AddRange(
            new RequestStatusHistory(InitialStatusHistoryId, request.Id, ServiceRequestStatus.New, RequesterId, CreatedAtUtc),
            new RequestStatusHistory(InProgressStatusHistoryId, request.Id, ServiceRequestStatus.InProgress, TechnicianId, StartedAtUtc));
        database.RequestComments.AddRange(
            new RequestComment(RequesterCommentId, request.Id, RequesterId, "The room is needed for the quarterly planning session next week.", RequesterCommentedAtUtc),
            new RequestComment(TechnicianCommentId, request.Id, TechnicianId, "A replacement lamp is in stock and installation is scheduled for tomorrow morning.", TechnicianCommentedAtUtc));
        database.AuditEvents.AddRange(
            new AuditEvent(CreatedAuditEventId, request.Id, "request-created", RequesterId, null, CreatedAtUtc),
            new AuditEvent(AssignedAuditEventId, request.Id, "request-assigned", ManagerId, TechnicianId, AssignedAtUtc),
            new AuditEvent(StatusAuditEventId, request.Id, "status-changed", TechnicianId, ServiceRequestStatus.InProgress.ToString(), StartedAtUtc),
            new AuditEvent(CommentAuditEventId, request.Id, "comment-added", TechnicianId, null, TechnicianCommentedAtUtc));
        await database.SaveChangesAsync(cancellationToken);
    }
}
