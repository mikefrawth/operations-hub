namespace OperationsHub.Domain.Entities;

public sealed class RequestType
{
    public const int NameMaximumLength = 100;
    public const int DescriptionMaximumLength = 500;

    private RequestType()
    {
    }

    public RequestType(Guid id, string name, string? description, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = ValidateName(name);
        Description = ValidateDescription(description);
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Update(string name, string? description)
    {
        Name = ValidateName(name);
        Description = ValidateDescription(description);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Request type name is required.", nameof(name));
        }

        if (name.Length > NameMaximumLength)
        {
            throw new ArgumentException($"Request type name cannot exceed {NameMaximumLength} characters.", nameof(name));
        }

        return name;
    }

    private static string? ValidateDescription(string? description)
    {
        if (description?.Length > DescriptionMaximumLength)
        {
            throw new ArgumentException($"Request type description cannot exceed {DescriptionMaximumLength} characters.", nameof(description));
        }

        return description;
    }
}
