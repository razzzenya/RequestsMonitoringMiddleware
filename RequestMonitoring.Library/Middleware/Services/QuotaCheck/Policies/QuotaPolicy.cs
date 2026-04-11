using Microsoft.Extensions.Caching.Distributed;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Базовый класс политики квоты
/// </summary>
public abstract class QuotaPolicy
{
    /// <summary>
    /// Инкрементирует счётчик и проверяет квоту
    /// </summary>
    public abstract Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDistributedCache cache, DomainListsContext dbContext, int syncEveryNRequests);

    /// <summary>
    /// Создаёт политику по типу квоты
    /// </summary>
    public static QuotaPolicy Create(QuotaType type) => type switch
    {
        QuotaType.Unlimited         => new UnlimitedQuotaPolicy(),
        QuotaType.Periodic          => new PeriodicQuotaPolicy(),
        QuotaType.Total             => new TotalQuotaPolicy(),
        QuotaType.ExpiringUnlimited => new ExpiringUnlimitedQuotaPolicy(),
        QuotaType.ExpiringTotal     => new ExpiringTotalQuotaPolicy(),
        QuotaType.ExpiringPeriodic  => new ExpiringPeriodicQuotaPolicy(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown quota type: {type}")
    };

    protected static string GetCacheKey(Quota quota) => $"Quota_{quota.DomainId}";

    protected static async Task<long> IncrementInRedisAsync(Quota quota, IDistributedCache cache, DistributedCacheEntryOptions options)
    {
        var cacheKey = GetCacheKey(quota);
        var cachedValue = await cache.GetStringAsync(cacheKey);

        var count = cachedValue is null ? quota.RequestCount + 1 : long.Parse(cachedValue) + 1;

        await cache.SetStringAsync(cacheKey, count.ToString(), options);
        return count;
    }

    protected static async Task SaveCounterAsync(Quota quota, DomainListsContext dbContext, long count, int syncEveryNRequests)
    {
        if (count % syncEveryNRequests == 0)
        {
            quota.RequestCount = count;
            await dbContext.SaveChangesAsync();
        }
    }
}