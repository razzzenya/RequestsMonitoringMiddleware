using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

public class QuotaService(IDistributedCache cache, DomainListsContext dbContext, IConfiguration configuration, ILogger<QuotaService> logger) : IQuotaService
{
    private readonly int _syncEveryNRequests = configuration.GetValue("QuotaSettings:SyncEveryNRequests", 10);

    public async Task<QuotaCheckResult> CheckAndIncrementAsync(string host)
    {
        var quota = await dbContext.Quotas
            .Include(q => q.Domain)
            .FirstOrDefaultAsync(q => q.Domain.Host == host);

        if (quota is null)
            return QuotaCheckResult.NoQuota;

        if (quota.ExpiresAt.HasValue && DateTime.UtcNow >= quota.ExpiresAt.Value)
        {
            logger.LogWarning("Quota expired for domain {Host}, expired at {ExpiresAt}", host, quota.ExpiresAt);
            return QuotaCheckResult.Exceeded;
        }

        if (!quota.MaxRequests.HasValue)
            return QuotaCheckResult.Allowed;

        var count = await IncrementCounterAsync(quota);

        if (count > quota.MaxRequests.Value)
        {
            logger.LogWarning("Quota exceeded for domain {Host}: {Count}/{Max}", host, count, quota.MaxRequests);
            return QuotaCheckResult.Exceeded;
        }

        logger.LogInformation("Quota check passed for domain {Host}: {Count}/{Max}", host, count, quota.MaxRequests);
        return QuotaCheckResult.Allowed;
    }

    private async Task<long> IncrementCounterAsync(Quota quota)
    {
        var cacheKey = GetCacheKey(quota);

        try
        {
            return await IncrementInRedisAsync(quota, cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for quota {QuotaId}, falling back to database", quota.Id);
            return await IncrementInDatabaseAsync(quota);
        }
    }

    private async Task<long> IncrementInRedisAsync(Quota quota, string cacheKey)
    {
        var cachedValue = await cache.GetStringAsync(cacheKey);

        long count;
        if (cachedValue is null)
        {
            count = await GetCountFromDatabase(quota);
            count++;
        }
        else
        {
            count = long.Parse(cachedValue) + 1;
        }

        var options = new DistributedCacheEntryOptions();
        if (quota.PeriodSeconds.HasValue)
        {
            if (quota.LastResetAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - quota.LastResetAt.Value;
                if (elapsed.TotalSeconds >= quota.PeriodSeconds.Value)
                {
                    count = 1;
                    quota.LastResetAt = DateTime.UtcNow;
                    quota.RequestCount = 0;
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Quota counter reset for domain {Host}, period {Period}s elapsed", quota.Domain.Host, quota.PeriodSeconds);
                }
            }
            else
            {
                quota.LastResetAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            var remaining = TimeSpan.FromSeconds(quota.PeriodSeconds.Value);
            if (quota.LastResetAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - quota.LastResetAt.Value;
                remaining = TimeSpan.FromSeconds(quota.PeriodSeconds.Value) - elapsed;
                if (remaining <= TimeSpan.Zero)
                    remaining = TimeSpan.FromSeconds(quota.PeriodSeconds.Value);
            }
            options.AbsoluteExpirationRelativeToNow = remaining;
        }

        await cache.SetStringAsync(cacheKey, count.ToString(), options);

        if (count % _syncEveryNRequests == 0)
        {
            await SyncCountToDatabase(quota, count);
        }

        return count;
    }

    private async Task<long> IncrementInDatabaseAsync(Quota quota)
    {
        if (quota.PeriodSeconds.HasValue && quota.LastResetAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - quota.LastResetAt.Value;
            if (elapsed.TotalSeconds >= quota.PeriodSeconds.Value)
            {
                quota.RequestCount = 0;
                quota.LastResetAt = DateTime.UtcNow;
                logger.LogInformation("Quota counter reset (DB fallback) for domain {Host}", quota.Domain.Host);
            }
        }
        else if (quota.PeriodSeconds.HasValue && !quota.LastResetAt.HasValue)
        {
            quota.LastResetAt = DateTime.UtcNow;
        }

        quota.RequestCount++;
        await dbContext.SaveChangesAsync();

        return quota.RequestCount;
    }

    private async Task<long> GetCountFromDatabase(Quota quota)
    {
        if (quota.PeriodSeconds.HasValue && quota.LastResetAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - quota.LastResetAt.Value;
            if (elapsed.TotalSeconds >= quota.PeriodSeconds.Value)
            {
                quota.RequestCount = 0;
                quota.LastResetAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                return 0;
            }
        }

        return quota.RequestCount;
    }

    private async Task SyncCountToDatabase(Quota quota, long count)
    {
        try
        {
            quota.RequestCount = count;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Synced quota counter to DB for domain {Host}: {Count}", quota.Domain.Host, count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync quota counter to DB for domain {Host}", quota.Domain.Host);
        }
    }

    private static string GetCacheKey(Quota quota) => $"Quota_{quota.DomainId}";
}
