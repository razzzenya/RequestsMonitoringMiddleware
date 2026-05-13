using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Сервис проверки статуса домена
/// </summary>
public class DomainCheckService(IConfiguration configuration, HybridCache cache, DomainListsContext dbcontext, ILogger<DomainCheckService> logger) : IDomainCheckService
{
    private readonly int cacheExpirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <summary>
    /// Проверяет статус домена из контекста запроса
    /// </summary>
    public async Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context)
    {
        var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() ?? context.Request.Host.Host;
        var cacheKey = $"Domain_{domain}";

        var status = await cache.GetOrCreateAsync(
            cacheKey,
            async ct => await GetDomainStatusFromDatabaseAsync(domain),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cacheExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes)
            }
        );

        logger.LogInformation("Domain {Domain} status: {Status}", domain, status.Name);
        return status;
    }

    /// <summary>
    /// Получает статус домена из базы данных
    /// </summary>
    private async Task<DomainStatusType> GetDomainStatusFromDatabaseAsync(string domain)
    {
        var domainEntity = await dbcontext.Domains
            .Include(d => d.DomainStatusType)
            .FirstOrDefaultAsync(d => d.Host == domain);

        if (domainEntity?.DomainStatusType != null)
        {
            logger.LogInformation("Domain {Domain} found in database with status {Status}",
                domain, domainEntity.DomainStatusType.Name);
            return domainEntity.DomainStatusType;
        }

        logger.LogWarning("Domain {Domain} not found in database. Returning blocked status", domain);
        return await dbcontext.DomainStatusTypes.FirstAsync(s => s.Id == 3);
    }
}
