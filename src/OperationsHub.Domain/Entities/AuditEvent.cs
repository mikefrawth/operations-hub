namespace OperationsHub.Domain.Entities;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public AuditEvent(Guid id, Guid? serviceRequestId, string eventType, string actorId, string? details, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        ServiceRequestId = serviceRequestId;
        EventType = eventType;
        ActorId = actorId;
        Details = details;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid? ServiceRequestId { get; private set; }

    public string EventType { get; private set; } = null!;

    public string ActorId { get; private set; } = null!;

    public string? Details { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }
}
