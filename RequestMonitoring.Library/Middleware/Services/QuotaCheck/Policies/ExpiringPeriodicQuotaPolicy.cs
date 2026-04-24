using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов в период до определённой даты
/// </summary>
public class ExpiringPeriodicQuotaPolicy : PeriodicQuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDatabase db, DomainListsContext dbContext, int syncEveryNRequests)
    {
        if (DateTime.UtcNow >= quota.ExpiresAt!.Value)
            return QuotaCheckResult.Exceeded; // срок действия истёк - Greylisted

        await ResetPeriodIfNeededAsync(quota, db, dbContext);

        var count = await IncrementInRedisAsync(quota, db);
        await SaveCounterAsync(quota, dbContext, count, syncEveryNRequests);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.TemporarilyExceeded // лимит периода исчерпан - 429, сбросится в следующем периоде
            : QuotaCheckResult.Allowed;
    }
}