using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов всего, счётчик никогда не сбрасывается
/// </summary>
public class TotalQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDatabase db, DomainListsContext dbContext, int syncEveryNRequests)
    {
        var count = await IncrementInRedisAsync(quota, db);
        await SaveCounterAsync(quota, dbContext, count, syncEveryNRequests);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;
    }
}