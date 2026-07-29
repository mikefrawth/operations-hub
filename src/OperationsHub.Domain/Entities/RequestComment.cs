namespace OperationsHub.Domain.Entities;

public sealed class RequestComment
{
    private RequestComment()
    {
    }

    public RequestComment(Guid id, Guid serviceRequestId, string authorId, string body, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ServiceRequestId = serviceRequestId;
        AuthorId = authorId;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ServiceRequestId { get; private set; }

    public string AuthorId { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
