using System.Text.Json;
using OperationsHub.Domain.Entities;

namespace OperationsHub.Application.Impersonation;

public sealed class AdministratorImpersonationService : IAdministratorImpersonationService
{
    private const string StartedEventType = "administrator-impersonation-started";
    private const string EndedEventType = "administrator-impersonation-ended";
    private readonly IImpersonationStore store;
    private readonly TimeProvider timeProvider;

    public AdministratorImpersonationService(IImpersonationStore store, TimeProvider timeProvider)
    {
        this.store = store;
        this.timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<ImpersonationTargetDto>> GetEligibleTargetsAsync(
        CancellationToken cancellationToken) =>
        store.GetEligibleTargetsAsync(timeProvider.GetUtcNow(), cancellationToken);

    public async Task<ImpersonationOperationResult> StartAsync(
        string administratorUserId,
        string targetUserId,
        ImpersonationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (string.IsNullOrWhiteSpace(administratorUserId)
            || !await store.IsActiveAdministratorAsync(administratorUserId, now, cancellationToken))
        {
            return Failure(ImpersonationOperationStatus.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(targetUserId)
            || string.Equals(administratorUserId, targetUserId, StringComparison.Ordinal))
        {
            return Failure(ImpersonationOperationStatus.InvalidTarget);
        }

        var target = await store.FindEligibleTargetAsync(targetUserId, now, cancellationToken);
        if (target is null)
        {
            return Failure(ImpersonationOperationStatus.InvalidTarget);
        }

        store.Add(new AuditEvent(
            Guid.NewGuid(),
            null,
            StartedEventType,
            administratorUserId,
            CreateStartedDetails(target, requestContext),
            now));
        await store.SaveChangesAsync(cancellationToken);

        return new ImpersonationOperationResult(ImpersonationOperationStatus.Success, target);
    }

    public async Task<ImpersonationOperationResult> EndAsync(
        string administratorUserId,
        string targetUserId,
        ImpersonationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (string.IsNullOrWhiteSpace(administratorUserId)
            || !await store.IsActiveAdministratorAsync(administratorUserId, now, cancellationToken))
        {
            return Failure(ImpersonationOperationStatus.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(targetUserId)
            || string.Equals(administratorUserId, targetUserId, StringComparison.Ordinal))
        {
            return Failure(ImpersonationOperationStatus.InvalidTarget);
        }

        store.Add(new AuditEvent(
            Guid.NewGuid(),
            null,
            EndedEventType,
            administratorUserId,
            CreateEndedDetails(targetUserId, requestContext),
            now));
        await store.SaveChangesAsync(cancellationToken);

        return new ImpersonationOperationResult(ImpersonationOperationStatus.Success, null);
    }

    private static ImpersonationOperationResult Failure(ImpersonationOperationStatus status) =>
        new(status, null);

    private static string CreateStartedDetails(
        ImpersonationTargetDto target,
        ImpersonationRequestContext requestContext) =>
        JsonSerializer.Serialize(new
        {
            TargetUserId = target.UserId,
            target.DisplayName,
            target.Role,
            RemoteIpAddress = Limit(requestContext.RemoteIpAddress, 100),
            TraceIdentifier = Limit(requestContext.TraceIdentifier, 200),
        });

    private static string CreateEndedDetails(
        string targetUserId,
        ImpersonationRequestContext requestContext) =>
        JsonSerializer.Serialize(new
        {
            TargetUserId = targetUserId,
            RemoteIpAddress = Limit(requestContext.RemoteIpAddress, 100),
            TraceIdentifier = Limit(requestContext.TraceIdentifier, 200),
        });

    private static string? Limit(string? value, int maximumLength) =>
        value?.Length > maximumLength ? value[..maximumLength] : value;
}
