namespace OperationsHub.Domain.Entities;

public sealed class RequestAssignment
{
    private RequestAssignment()
    {
    }

    public RequestAssignment(Guid id, Guid serviceRequestId, string assigneeId, string assignedById, DateTimeOffset assignedAtUtc)
    {
        Id = id;
        ServiceRequestId = serviceRequestId;
        AssigneeId = assigneeId;
        AssignedById = assignedById;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ServiceRequestId { get; private set; }

    public string AssigneeId { get; private set; } = null!;

    public string AssignedById { get; private set; } = null!;

    public DateTimeOffset AssignedAtUtc { get; private set; }
}
