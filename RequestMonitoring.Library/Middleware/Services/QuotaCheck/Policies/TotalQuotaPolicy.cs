using Microsoft.Extensions.Caching.Distributed;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов всего, счётчик никогда не сбрасывается
/// </summary>
public class TotalQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDistributedCache cache, DomainListsContext dbContext, int syncEveryNRequests)
    {
        var options = new DistributedCacheEntryOptions();
        var count = await IncrementInRedisAsync(quota, cache, options);
        await SaveCounterAsync(quota, dbContext, count, syncEveryNRequests);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;
    }
}