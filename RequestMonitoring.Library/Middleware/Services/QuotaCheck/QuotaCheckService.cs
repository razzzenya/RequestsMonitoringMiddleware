using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Dto;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;
using RequestMonitoring.Library.Shared;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

/// <summary>
/// Сервис проверки квоты домена. Использует кэшированные метаданные квоты,
/// для Periodic-типов дополнительно читает LastResetAt из БД.
/// </summary>
public class QuotaCheckService(IConnectionMultiplexer redis, DomainListsContext dbContext, IConfiguration configuration, IDomainCacheService domainCacheService, IQuotaCacheService quotaCacheService, ILogger<QuotaCheckService> logger) : IQuotaCheckService
{
    private readonly int _syncEveryNRequests = configuration.GetValue("QuotaSettings:SyncEveryNRequests", 10);

    /// <inheritdoc/>
    public async Task<QuotaCheckResult> CheckAndIncrementAsync(string host, QuotaMetaDto? quotaMeta)
    {
        if (quotaMeta is null)
            return QuotaCheckResult.NoQuota;

        var quota = await BuildQuotaAsync(quotaMeta);

        var db = redis.GetDatabase();
        var policy = QuotaPolicy.Create(quota.Type);
        var result = await policy.ExecuteAsync(quota, db, dbContext, _syncEveryNRequests);

        if (result == QuotaCheckResult.Exceeded)
        {
            logger.LogWarning("Quota exceeded for domain {Host}, moving to Greylisted", host);
            await MoveToGreylistedAsync(host, quotaMeta.DomainId);
        }
        else if (result == QuotaCheckResult.TemporarilyExceeded)
        {
            logger.LogWarning("Periodic quota temporarily exceeded for domain {Host}", host);
        }

        return result;
    }

    private async Task<Quota> BuildQuotaAsync(QuotaMetaDto meta)
    {
        DateTime? lastResetAt = null;
        long requestCount = 0;

        if (meta.Type is QuotaType.Periodic or QuotaType.ExpiringPeriodic)
        {
            var quotas = await dbContext.Quotas
                .Where(q => q.Id == meta.Id)
                .Select(q => new { q.LastResetAt, q.RequestCount })
                .FirstOrDefaultAsync();

            lastResetAt = quotas?.LastResetAt;
            requestCount = quotas?.RequestCount ?? 0;
        }

        return new Quota
        {
            Id = meta.Id,
            DomainId = meta.DomainId,
            Domain = null!,
            Type = meta.Type,
            MaxRequests = meta.MaxRequests,
            PeriodSeconds = meta.PeriodSeconds,
            ExpiresAt = meta.ExpiresAt,
            RequestCount = requestCount,
            LastResetAt = lastResetAt
        };
    }

    private async Task MoveToGreylistedAsync(string host, int domainId)
    {
        try
        {
            var affected = await dbContext.Domains
                .Where(d => d.Id == domainId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.DomainStatusTypeId, 2));

            if (affected == 0)
                return;

            await Task.WhenAll(
                domainCacheService.InvalidateDomainAsync(host),
                quotaCacheService.DeleteCounterAsync(domainId)
            );

            logger.LogWarning("Domain {Host} moved to Greylisted due to quota exceeded", host);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move domain {Host} to Greylisted", host);
        }
    }
}
