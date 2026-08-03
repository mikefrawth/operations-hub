using OperationsHub.Domain.Entities;

namespace OperationsHub.Application.Impersonation;

public interface IImpersonationStore
{
    public Task<IReadOnlyList<ImpersonationTargetDto>> GetEligibleTargetsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    public Task<ImpersonationTargetDto?> FindEligibleTargetAsync(
        string userId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    public Task<bool> IsActiveAdministratorAsync(
        string userId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    public void Add(AuditEvent auditEvent);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
