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

    [Fact]
    public void AdvanceVersionIncrementsTheConcurrencyToken()
    {
        var request = new ServiceRequest(Guid.NewGuid(), "SR-20260729-000000000000000000", "Laptop setup", "Prepare a laptop for a new employee.", "requester", Guid.NewGuid(), ServiceRequestPriority.Normal, DateTimeOffset.UtcNow);

        request.AdvanceVersion();

        Assert.Equal((uint)1, request.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsMissingTitles(string title)
    {
        Assert.Throws<ArgumentException>(() => new ServiceRequest(
            Guid.NewGuid(),
            "SR-TEST-INVARIANT",
            title,
            "A valid description.",
            "requester",
            Guid.NewGuid(),
            ServiceRequestPriority.Normal,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ConstructorRejectsValuesThatCannotBePersisted()
    {
        Assert.Throws<ArgumentException>(() => new ServiceRequest(
            Guid.NewGuid(),
            new string('R', ServiceRequest.RequestNumberMaximumLength + 1),
            "Title",
            "Description",
            "requester",
            Guid.NewGuid(),
            ServiceRequestPriority.Normal,
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServiceRequest(
            Guid.NewGuid(),
            "SR-TEST-INVARIANT",
            "Title",
            "Description",
            "requester",
            Guid.NewGuid(),
            (ServiceRequestPriority)999,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UpdateRejectsEmptyReferencesAndOversizedText()
    {
        var request = new ServiceRequest(Guid.NewGuid(), "SR-TEST-INVARIANT", "Title", "Description", "requester", Guid.NewGuid(), ServiceRequestPriority.Normal, DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => request.Update(new string('T', ServiceRequest.TitleMaximumLength + 1), "Description", Guid.NewGuid(), ServiceRequestPriority.Normal, null, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => request.Update("Title", "Description", Guid.Empty, ServiceRequestPriority.Normal, null, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => request.Update("Title", "Description", Guid.NewGuid(), ServiceRequestPriority.Normal, Guid.Empty, DateTimeOffset.UtcNow));
    }
}
