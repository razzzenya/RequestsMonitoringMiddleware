using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов в период, бессрочно
/// </summary>
public class PeriodicQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDatabase db, DomainListsContext dbContext, int syncEveryNRequests)
    {
        await ResetPeriodIfNeededAsync(quota, db, dbContext);

        var count = await IncrementInRedisAsync(quota, db);
        await SaveCounterAsync(quota, dbContext, count, syncEveryNRequests);

        return count > quota.MaxRequests!.Value ? QuotaCheckResult.TemporarilyExceeded : QuotaCheckResult.Allowed;
    }

    protected static async Task ResetPeriodIfNeededAsync(Quota quota, IDatabase db, DomainListsContext dbContext)
    {
        var period = TimeSpan.FromSeconds(quota.PeriodSeconds!.Value);
        var now = DateTime.UtcNow;

        if (!quota.LastResetAt.HasValue)
        {
            await dbContext.Quotas
                .Where(q => q.Id == quota.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.LastResetAt, now));
            quota.LastResetAt = now;
            return;
        }

        if (now - quota.LastResetAt.Value >= period)
        {
            await dbContext.Quotas
                .Where(q => q.Id == quota.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.LastResetAt, now)
                    .SetProperty(q => q.RequestCount, 0));
            quota.LastResetAt = now;
            quota.RequestCount = 0;

            // Reset the Redis counter so the new period starts from zero
            await DeleteCacheKeyAsync(quota, db);
        }
    }
}