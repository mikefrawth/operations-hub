using OperationsHub.Domain.Enums;

namespace OperationsHub.Domain.Entities;

public sealed class ServiceRequest
{
    private ServiceRequest()
    {
    }

    public ServiceRequest(
        Guid id,
        string requestNumber,
        string title,
        string description,
        string requesterId,
        Guid requestTypeId,
        ServiceRequestPriority priority,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        RequestNumber = requestNumber;
        Title = title;
        Description = description;
        RequesterId = requesterId;
        RequestTypeId = requestTypeId;
        Priority = priority;
        Status = ServiceRequestStatus.New;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string RequestNumber { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string RequesterId { get; private set; } = null!;

    public Guid RequestTypeId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public string? AssigneeId { get; private set; }

    public ServiceRequestStatus Status { get; private set; }

    public ServiceRequestPriority Priority { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public uint Version { get; private set; }
}
