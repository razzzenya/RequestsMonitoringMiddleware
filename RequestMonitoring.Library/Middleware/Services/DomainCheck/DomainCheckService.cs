using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites.Domain;
using System.Text.Json;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Сервис проверки статуса домена
/// </summary>
public class DomainCheckService(IConfiguration configuration, IDistributedCache cache, DomainListsContext dbcontext, ILogger<DomainCheckService> logger) : IDomainCheckService
{
    private readonly int cacheExpirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);
    
    /// <summary>
    /// Проверяет статус домена из контекста запроса
    /// </summary>
    public async Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context)
    {
        var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() ?? context.Request.Host.Host;
        var cacheKey = $"Domain_{domain}";
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var deserializedStatus = JsonSerializer.Deserialize<DomainStatusType>(cachedData);
                if (deserializedStatus != null)
                {
                    logger.LogDebug("Domain {Domain} status loaded from cache", domain);
                    return deserializedStatus;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read from cache for domain {Domain}. Falling back to database", domain);
        }

        var domainStatus = await GetDomainStatusFromDatabaseAsync(domain);

        await TryCacheResultAsync(cacheKey, domainStatus);

        return domainStatus;
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
            logger.LogDebug("Domain {Domain} found in database with status {Status}", 
                domain, domainEntity.DomainStatusType.Name);
            return domainEntity.DomainStatusType;
        }

        logger.LogDebug("Domain {Domain} not found in database. Returning blocked status", domain);
        return await dbcontext.DomainStatusTypes.FirstAsync(s => s.Id == 3); ;
    }

    /// <summary>
    /// Пытается сохранить результат в кеш
    /// </summary>
    private async Task TryCacheResultAsync(string cacheKey, DomainStatusType status)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(status);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationMinutes)
            };
            
            await cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
            logger.LogDebug("Cached domain status with key {CacheKey} for {Minutes} minutes", 
                cacheKey, cacheExpirationMinutes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache result for key {CacheKey}", cacheKey);
        }
    }
}
