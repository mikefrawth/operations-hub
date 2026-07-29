using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.UnitTests.Requests;

public sealed class ServiceRequestTests
{
    [Fact]
    public void ChangeStatusPermitsWorkflowTransitionAndRejectsInvalidTransition()
    {
        var createdAt = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
        var request = new ServiceRequest(Guid.NewGuid(), "SR-20260729-000000000000000000", "Laptop setup", "Prepare a laptop for a new employee.", "requester", Guid.NewGuid(), ServiceRequestPriority.Normal, createdAt);

        request.ChangeStatus(ServiceRequestStatus.InProgress, createdAt.AddMinutes(1));

        Assert.Equal(ServiceRequestStatus.InProgress, request.Status);
        Assert.Equal(createdAt.AddMinutes(1), request.UpdatedAtUtc);
        Assert.Throws<InvalidOperationException>(() => request.ChangeStatus(ServiceRequestStatus.New, createdAt.AddMinutes(2)));
    }

    [Fact]
    public void UpdateRejectsResolvedRequest()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var request = new ServiceRequest(Guid.NewGuid(), "SR-20260729-000000000000000000", "Laptop setup", "Prepare a laptop for a new employee.", "requester", Guid.NewGuid(), ServiceRequestPriority.Normal, createdAt);
        request.ChangeStatus(ServiceRequestStatus.InProgress, createdAt.AddMinutes(1));
        request.ChangeStatus(ServiceRequestStatus.Resolved, createdAt.AddMinutes(2));

        Assert.Throws<InvalidOperationException>(() => request.Update("Revised", "Revised description", Guid.NewGuid(), ServiceRequestPriority.High, null, createdAt.AddMinutes(3)));
    }
}
