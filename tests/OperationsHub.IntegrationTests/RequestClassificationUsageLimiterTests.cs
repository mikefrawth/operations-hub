using OperationsHub.Web.Api;

namespace OperationsHub.IntegrationTests;

public sealed class RequestClassificationUsageLimiterTests
{
    [Fact]
    public void LimiterEnforcesTheConfiguredPerUserQuota()
    {
        using var limiter = new InMemoryRequestClassificationUsageLimiter();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            Assert.True(limiter.TryAcquire("requester-a"));
        }

        Assert.False(limiter.TryAcquire("requester-a"));
        Assert.True(limiter.TryAcquire("requester-b"));
    }
}
