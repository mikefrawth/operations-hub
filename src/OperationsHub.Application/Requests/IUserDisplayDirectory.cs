namespace OperationsHub.Application.Requests;

public interface IUserDisplayDirectory
{
    public Task<IReadOnlyList<UserDisplayDto>> GetByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);
}
