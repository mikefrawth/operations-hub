namespace OperationsHub.Domain.Entities;

public sealed class Department
{
    private Department()
    {
    }

    public Department(Guid id, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
