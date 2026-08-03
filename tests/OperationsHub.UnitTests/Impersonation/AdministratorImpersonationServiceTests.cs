using System.Text.Json;
using OperationsHub.Application.Impersonation;
using OperationsHub.Domain.Entities;

namespace OperationsHub.UnitTests.Impersonation;

public sealed class AdministratorImpersonationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task EligibleTargetsUseTheCurrentTimeForAccountAvailability()
    {
        var store = new InMemoryImpersonationStore();
        store.Targets.Add(CreateTarget());
        var service = CreateService(store);

        var targets = await service.GetEligibleTargetsAsync(CancellationToken.None);

        Assert.Single(targets);
        Assert.Equal(Now, store.LastAsOfUtc);
    }

    [Fact]
    public async Task StartRejectsAnActorWhoIsNotAnActiveAdministrator()
    {
        var store = new InMemoryImpersonationStore { IsAdministrator = false };
        store.Targets.Add(CreateTarget());
        var service = CreateService(store);

        var result = await service.StartAsync(
            "administrator-id",
            "target-id",
            CreateRequestContext(),
            CancellationToken.None);

        Assert.Equal(ImpersonationOperationStatus.Forbidden, result.Status);
        Assert.Empty(store.AuditEvents);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task StartRejectsAUserWhoIsNotAnEligibleTarget()
    {
        var store = new InMemoryImpersonationStore();
        var service = CreateService(store);

        var result = await service.StartAsync(
            "administrator-id",
            "administrator-target-id",
            CreateRequestContext(),
            CancellationToken.None);

        Assert.Equal(ImpersonationOperationStatus.InvalidTarget, result.Status);
        Assert.Empty(store.AuditEvents);
    }

    [Fact]
    public async Task StartPersistsAnAuditEventBeforeReturningTheTarget()
    {
        var store = new InMemoryImpersonationStore();
        store.Targets.Add(CreateTarget());
        var service = CreateService(store);

        var result = await service.StartAsync(
            "administrator-id",
            "target-id",
            CreateRequestContext(),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("target-id", result.Target!.UserId);
        var auditEvent = Assert.Single(store.AuditEvents);
        Assert.Equal("administrator-impersonation-started", auditEvent.EventType);
        Assert.Equal("administrator-id", auditEvent.ActorId);
        Assert.Equal(Now, auditEvent.OccurredAtUtc);
        Assert.Equal(1, store.SaveCount);

        using var details = JsonDocument.Parse(auditEvent.Details!);
        Assert.Equal(
            "target-id",
            details.RootElement.GetProperty("TargetUserId").GetString());
        Assert.Equal(
            "Technician",
            details.RootElement.GetProperty("Role").GetString());
        Assert.Equal(
            "127.0.0.1",
            details.RootElement.GetProperty("RemoteIpAddress").GetString());
    }

    [Fact]
    public async Task EndRevalidatesTheAdministratorAndAuditsTheTargetSession()
    {
        var store = new InMemoryImpersonationStore();
        var service = CreateService(store);

        var result = await service.EndAsync(
            "administrator-id",
            "target-id",
            CreateRequestContext(),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var auditEvent = Assert.Single(store.AuditEvents);
        Assert.Equal("administrator-impersonation-ended", auditEvent.EventType);
        Assert.Equal("administrator-id", auditEvent.ActorId);
        using var details = JsonDocument.Parse(auditEvent.Details!);
        Assert.Equal(
            "target-id",
            details.RootElement.GetProperty("TargetUserId").GetString());
    }

    private static AdministratorImpersonationService CreateService(
        InMemoryImpersonationStore store) =>
        new(store, new TestTimeProvider(Now));

    private static ImpersonationTargetDto CreateTarget() =>
        new(
            "target-id",
            "Demo Technician",
            "technician@operationshub.local",
            "Technician");

    private static ImpersonationRequestContext CreateRequestContext() =>
        new("127.0.0.1", "trace-id");

    private sealed class InMemoryImpersonationStore : IImpersonationStore
    {
        public List<ImpersonationTargetDto> Targets { get; } = [];

        public List<AuditEvent> AuditEvents { get; } = [];

        public bool IsAdministrator { get; init; } = true;

        public DateTimeOffset? LastAsOfUtc { get; private set; }

        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<ImpersonationTargetDto>> GetEligibleTargetsAsync(
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            LastAsOfUtc = asOfUtc;
            return Task.FromResult<IReadOnlyList<ImpersonationTargetDto>>(Targets);
        }

        public Task<ImpersonationTargetDto?> FindEligibleTargetAsync(
            string userId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            LastAsOfUtc = asOfUtc;
            return Task.FromResult(Targets.SingleOrDefault(target => target.UserId == userId));
        }

        public Task<bool> IsActiveAdministratorAsync(
            string userId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            LastAsOfUtc = asOfUtc;
            return Task.FromResult(IsAdministrator);
        }

        public void Add(AuditEvent auditEvent) => AuditEvents.Add(auditEvent);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
