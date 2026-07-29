using OperationsHub.Domain.Enums;

namespace OperationsHub.Domain.Entities;

public sealed class RequestStatusHistory
{
    private RequestStatusHistory()
    {
    }

    public RequestStatusHistory(Guid id, Guid serviceRequestId, ServiceRequestStatus status, string changedById, DateTimeOffset changedAtUtc)
    {
        Id = id;
        ServiceRequestId = serviceRequestId;
        Status = status;
        ChangedById = changedById;
        ChangedAtUtc = changedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ServiceRequestId { get; private set; }

    public ServiceRequestStatus Status { get; private set; }

    public string ChangedById { get; private set; } = null!;

    public DateTimeOffset ChangedAtUtc { get; private set; }
}
