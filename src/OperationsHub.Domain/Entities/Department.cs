namespace OperationsHub.Domain.Entities;

public sealed class Department
{
    private Department()
    {
    }

    public Department(Guid id, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = ValidateName(name);
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Rename(string name)
    {
        Name = ValidateName(name);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Department name is required.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Department name cannot exceed 100 characters.", nameof(name));
        }

        return name;
    }
}
