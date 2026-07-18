using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Dto;
using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Сервис проверки статуса домена с кэшированием
/// </summary>
public class DomainCheckService(IConfiguration configuration, HybridCache cache, DomainListsContext dbcontext, ILogger<DomainCheckService> logger) : IDomainCheckService
{
    private readonly int cacheExpirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <inheritdoc/>
    public async Task<DomainCacheEntry> IsDomainAllowedAsync(HttpContext context)
    {
        var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() ?? context.Request.Host.Host;
        var cacheKey = $"Domain_{domain}";

        var entry = await cache.GetOrCreateAsync(
            cacheKey,
            async ct => await GetDomainEntryFromDatabaseAsync(domain, ct),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cacheExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes)
            },
            cancellationToken: context.RequestAborted
        );

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Domain {Domain} status: {Status}", LogSanitizer.Sanitize(domain), entry.Status);
        return entry;
    }

    private async Task<DomainCacheEntry> GetDomainEntryFromDatabaseAsync(string domain, CancellationToken ct)
    {
        var domainEntity = await dbcontext.Domains
            .Include(d => d.DomainStatusType)
            .Include(d => d.Quota)
            .FirstOrDefaultAsync(d => d.Host == domain, ct);

        if (domainEntity?.DomainStatusType is null)
        {
            logger.LogWarning("Domain {Domain} not found in database", LogSanitizer.Sanitize(domain));
            return new DomainCacheEntry(DomainStatus.Forbidden, null);
        }

        var status = domainEntity.DomainStatusType.Id switch
        {
            1 => DomainStatus.Allowed,
            2 => DomainStatus.Greylisted,
            _ => DomainStatus.Forbidden
        };

        QuotaMetaDto? quotaMeta = domainEntity.Quota is { } q
            ? new QuotaMetaDto(q.Id, q.DomainId, q.Type, q.MaxRequests, q.PeriodSeconds, q.ExpiresAt)
            : null;

        return new DomainCacheEntry(status, quotaMeta);
    }

}
