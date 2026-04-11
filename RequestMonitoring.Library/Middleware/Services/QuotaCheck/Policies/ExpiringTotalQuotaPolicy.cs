using Microsoft.Extensions.Caching.Distributed;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов всего до определённой даты
/// </summary>
public class ExpiringTotalQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDistributedCache cache, DomainListsContext dbContext, int syncEveryNRequests)
    {
        if (DateTime.UtcNow >= quota.ExpiresAt!.Value)
            return QuotaCheckResult.Exceeded;

        var count = await IncrementInRedisAsync(quota, cache, new DistributedCacheEntryOptions());
        await SaveCounterAsync(quota, dbContext, count, syncEveryNRequests);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;
    }
}