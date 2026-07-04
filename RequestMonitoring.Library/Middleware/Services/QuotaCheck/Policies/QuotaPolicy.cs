using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Shared;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Базовый класс политики квоты
/// </summary>
public abstract class QuotaPolicy
{
    /// <summary>
    /// Инкрементирует счётчик и проверяет квоту
    /// </summary>
    public abstract Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDatabase db, DomainListsContext dbContext, int syncEveryNRequests);

    /// <summary>
    /// Создаёт политику по типу квоты
    /// </summary>
    public static QuotaPolicy Create(QuotaType type) => type switch
    {
        QuotaType.Unlimited => new UnlimitedQuotaPolicy(),
        QuotaType.Periodic => new PeriodicQuotaPolicy(),
        QuotaType.Total => new TotalQuotaPolicy(),
        QuotaType.ExpiringUnlimited => new ExpiringUnlimitedQuotaPolicy(),
        QuotaType.ExpiringTotal => new ExpiringTotalQuotaPolicy(),
        QuotaType.ExpiringPeriodic => new ExpiringPeriodicQuotaPolicy(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown quota type: {type}")
    };

    protected static string GetCacheKey(Quota quota) => $"Quota_{quota.DomainId}";

    protected static async Task<long> IncrementInRedisAsync(Quota quota, IDatabase db)
    {
        var cacheKey = GetCacheKey(quota);

        await db.StringSetAsync(cacheKey, quota.RequestCount, when: When.NotExists);

        return await db.StringIncrementAsync(cacheKey);
    }

    protected static async Task DeleteCacheKeyAsync(Quota quota, IDatabase db)
    {
        await db.KeyDeleteAsync(GetCacheKey(quota));
    }

    protected static async Task SaveCounterAsync(Quota quota, DomainListsContext dbContext, long count, int syncEveryNRequests)
    {
        if (count % syncEveryNRequests == 0)
        {
            await dbContext.Quotas
                .Where(q => q.Id == quota.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.RequestCount, count));
        }
    }
}