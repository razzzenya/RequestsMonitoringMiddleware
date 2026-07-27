using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Dto;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;
using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

/// <summary>
/// Сервис проверки квоты домена. Runtime-состояние квоты (request_count, last_reset_at) хранится в Redis,
/// SQLite используется только для persistence (метаданные и редкие обновления статуса).
/// </summary>
public class QuotaCheckService(IQuotaCounter quotaCounter, DomainListsContext dbContext, IDomainCacheService domainCacheService, IQuotaCacheService quotaCacheService, ILogger<QuotaCheckService> logger) : IQuotaCheckService
{
    /// <inheritdoc/>
    public async Task<QuotaCheckResult> CheckAndIncrementAsync(string host, QuotaMetaDto? quotaMeta)
    {
        if (quotaMeta is null)
            return QuotaCheckResult.NoQuota;

        var quota = BuildQuota(quotaMeta);

        var policy = QuotaPolicy.Create(quota.Type);
        var result = await policy.ExecuteAsync(quota, quotaCounter);

        if (result == QuotaCheckResult.Exceeded)
        {
            logger.LogWarning("Quota exceeded for domain {Host}, moving to Greylisted", LogSanitizer.Sanitize(host));
            await MoveToGreylistedAsync(host, quotaMeta.DomainId);
        }
        else if (result == QuotaCheckResult.TemporarilyExceeded)
        {
            logger.LogWarning("Periodic quota temporarily exceeded for domain {Host}", LogSanitizer.Sanitize(host));
        }

        return result;
    }

    private static Quota BuildQuota(QuotaMetaDto meta) => new()
    {
        Id = meta.Id,
        DomainId = meta.DomainId,
        Domain = null!,
        Type = meta.Type,
        MaxRequests = meta.MaxRequests,
        PeriodSeconds = meta.PeriodSeconds,
        ExpiresAt = meta.ExpiresAt,
        RequestCount = 0,
        LastResetAt = null
    };

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
                quotaCacheService.InvalidateQuotaAsync(domainId)
            );

            logger.LogWarning("Domain {Host} moved to Greylisted due to quota exceeded", LogSanitizer.Sanitize(host));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move domain {Host} to Greylisted", LogSanitizer.Sanitize(host));
        }
    }
}
