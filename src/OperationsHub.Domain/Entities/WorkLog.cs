namespace OperationsHub.Domain.Entities;

public sealed class WorkLog
{
    private WorkLog()
    {
    }

    public WorkLog(Guid id, Guid serviceRequestId, string authorId, decimal hours, string? note, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ServiceRequestId = serviceRequestId;
        AuthorId = authorId;
        Hours = hours;
        Note = note;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ServiceRequestId { get; private set; }

    public string AuthorId { get; private set; } = null!;

    public decimal Hours { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
