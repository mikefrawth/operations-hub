namespace OperationsHub.Application.Impersonation;

public interface IAdministratorImpersonationService
{
    public Task<IReadOnlyList<ImpersonationTargetDto>> GetEligibleTargetsAsync(
        CancellationToken cancellationToken);

    public Task<ImpersonationOperationResult> StartAsync(
        string administratorUserId,
        string targetUserId,
        ImpersonationRequestContext requestContext,
        CancellationToken cancellationToken);

    public Task<ImpersonationOperationResult> EndAsync(
        string administratorUserId,
        string targetUserId,
        ImpersonationRequestContext requestContext,
        CancellationToken cancellationToken);
}
