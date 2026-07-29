namespace OperationsHub.Domain.Entities;

public sealed class RequestType
{
    private RequestType()
    {
    }

    public RequestType(Guid id, string name, string? description, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
