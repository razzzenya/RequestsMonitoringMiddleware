using Microsoft.Extensions.Caching.Distributed;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов в период, бессрочно
/// </summary>
public class PeriodicQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDistributedCache cache, DomainListsContext dbContext, int syncEveryNRequests)
    {
        await ResetPeriodIfNeededAsync(quota, dbContext);

        var count = await IncrementInRedisAsync(quota, cache, new DistributedCacheEntryOptions());
        await SaveCounterAsync(quota, dbContext, count, syncEveryNRequests);

        return count > quota.MaxRequests!.Value ? QuotaCheckResult.TemporarilyExceeded : QuotaCheckResult.Allowed;
    }

    protected static async Task ResetPeriodIfNeededAsync(Quota quota, DomainListsContext dbContext)
    {
        var period = TimeSpan.FromSeconds(quota.PeriodSeconds!.Value);

        if (!quota.LastResetAt.HasValue)
        {
            quota.LastResetAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return;
        }

        if (DateTime.UtcNow - quota.LastResetAt.Value >= period)
        {
            quota.LastResetAt = DateTime.UtcNow;
            quota.RequestCount = 0;
            await dbContext.SaveChangesAsync();
        }
    }
}