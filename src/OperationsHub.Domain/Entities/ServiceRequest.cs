using OperationsHub.Domain.Enums;

namespace OperationsHub.Domain.Entities;

public sealed class ServiceRequest
{
    public const int TitleMaximumLength = 200;
    public const int DescriptionMaximumLength = 4_000;
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

    public void Update(string title, string description, Guid requestTypeId, ServiceRequestPriority priority, Guid? departmentId, DateTimeOffset updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (Status is ServiceRequestStatus.Resolved or ServiceRequestStatus.Closed)
        {
            throw new InvalidOperationException("Resolved and closed requests cannot be edited.");
        }

        Title = title;
        Description = description;
        RequestTypeId = requestTypeId;
        Priority = priority;
        DepartmentId = departmentId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Assign(string assigneeId, DateTimeOffset updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assigneeId);
        if (Status == ServiceRequestStatus.Closed)
        {
            throw new InvalidOperationException("Closed requests cannot be assigned.");
        }

        AssigneeId = assigneeId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ChangeStatus(ServiceRequestStatus status, DateTimeOffset updatedAtUtc)
    {
        if (!IsPermittedTransition(Status, status))
        {
            throw new InvalidOperationException($"A request cannot transition from {Status} to {status}.");
        }

        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static bool IsPermittedTransition(ServiceRequestStatus current, ServiceRequestStatus next) =>
        current switch
        {
            ServiceRequestStatus.New => next is ServiceRequestStatus.InProgress or ServiceRequestStatus.OnHold or ServiceRequestStatus.Closed,
            ServiceRequestStatus.InProgress => next is ServiceRequestStatus.OnHold or ServiceRequestStatus.Resolved or ServiceRequestStatus.Closed,
            ServiceRequestStatus.OnHold => next is ServiceRequestStatus.InProgress or ServiceRequestStatus.Closed,
            ServiceRequestStatus.Resolved => next is ServiceRequestStatus.InProgress or ServiceRequestStatus.Closed,
            _ => false,
        };
}
