using System.Threading.RateLimiting;
using OperationsHub.Application.Requests;

namespace OperationsHub.Web.Api;

public sealed class InMemoryRequestClassificationUsageLimiter : IRequestClassificationUsageLimiter, IDisposable
{
    private readonly PartitionedRateLimiter<string> limiter = PartitionedRateLimiter.Create<string, string>(userId =>
        RateLimitPartition.GetFixedWindowLimiter(
            userId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    public bool TryAcquire(string userId)
    {
        using var lease = limiter.AttemptAcquire(userId);
        return lease.IsAcquired;
    }

    public void Dispose() => limiter.Dispose();
}
