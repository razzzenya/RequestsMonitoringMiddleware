using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

/// <summary>
/// Redis-реализация счётчика квоты runtime-состояние (request_count, last_reset_at) хранится в Redis
/// </summary>
public class RedisQuotaCounter(IConnectionMultiplexer redis) : IQuotaCounter
{
    private static string CountKey(int domainId) => $"Quota:{domainId}";
    private static string LastResetKey(int domainId) => $"Quota:{domainId}:lastReset";

    public async Task<long> IncrementTotalAsync(int domainId, long seedRequestCount)
    {
        var db = redis.GetDatabase();
        var countKey = CountKey(domainId);

        await db.StringSetAsync(countKey, seedRequestCount, when: When.NotExists);
        return await db.StringIncrementAsync(countKey);
    }

    public async Task<long> IncrementPeriodicAsync(int domainId, int periodSeconds, long seedRequestCount)
    {
        var db = redis.GetDatabase();
        var countKey = CountKey(domainId);
        var lastResetKey = LastResetKey(domainId);

        await ResetPeriodIfNeededAsync(db, countKey, lastResetKey, periodSeconds);
        await db.StringSetAsync(countKey, seedRequestCount, when: When.NotExists);
        return await db.StringIncrementAsync(countKey);
    }

    private static async Task ResetPeriodIfNeededAsync(IDatabase db, RedisKey countKey, RedisKey lastResetKey, int periodSeconds)
    {
        var period = TimeSpan.FromSeconds(periodSeconds);
        var now = DateTime.UtcNow;

        var lastResetValue = await db.StringGetAsync(lastResetKey);
        if (!lastResetValue.HasValue)
        {
            await db.StringSetAsync(lastResetKey, now.Ticks);
            return;
        }

        var lastReset = new DateTime((long)lastResetValue);
        if (now - lastReset >= period)
        {
            await db.StringSetAsync(countKey, 0);
            await db.StringSetAsync(lastResetKey, now.Ticks);
        }
    }

    public async Task DeleteAsync(int domainId)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync([CountKey(domainId), LastResetKey(domainId)]);
    }
}
