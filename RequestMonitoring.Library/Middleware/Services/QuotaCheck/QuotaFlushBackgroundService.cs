using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

/// <summary>
/// Фоновый сервис, который периодически сбрасывает runtime-состояние квот из Redis в SQLite
/// </summary>
public class QuotaFlushBackgroundService(IConnectionMultiplexer redis, IServiceScopeFactory scopeFactory, ILogger<QuotaFlushBackgroundService> logger) : BackgroundService
{
    private const string QuotaKeyPrefix = "Quota:";
    private const string LastResetSuffix = ":lastReset";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to flush quota state to database");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task FlushAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        var endpoints = redis.GetEndPoints();
        if (endpoints.Length == 0)
            return;

        var server = redis.GetServer(endpoints[0]);
        var countKeys = new List<RedisKey>();

        await foreach (var key in server.KeysAsync(pattern: $"{QuotaKeyPrefix}*", pageSize: 1000).WithCancellation(stoppingToken))
        {
            var keyString = key.ToString();
            if (!keyString.EndsWith(LastResetSuffix))
                countKeys.Add(key);
        }

        if (countKeys.Count == 0)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<DomainListsContext>();

        foreach (var countKey in countKeys)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var keyString = countKey.ToString();
            if (!TryParseDomainId(keyString, out var domainId))
                continue;

            var countValue = await db.StringGetAsync(countKey);
            if (!countValue.HasValue)
                continue;

            var lastResetValue = await db.StringGetAsync($"{QuotaKeyPrefix}{domainId}{LastResetSuffix}");
            DateTime? lastReset = lastResetValue.HasValue
                ? new DateTime((long)lastResetValue)
                : null;

            await dbContext.Quotas
                .Where(q => q.DomainId == domainId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.RequestCount, (long)countValue)
                    .SetProperty(q => q.LastResetAt, lastReset),
                    stoppingToken);
        }

        logger.LogDebug("Flushed {Count} quota states to database", countKeys.Count);
    }

    private static bool TryParseDomainId(string key, out int domainId)
    {
        // key format: Quota:{domainId}
        var prefix = QuotaKeyPrefix;
        domainId = 0;
        if (!key.StartsWith(prefix))
            return false;

        return int.TryParse(key[prefix.Length..], out domainId);
    }
}
