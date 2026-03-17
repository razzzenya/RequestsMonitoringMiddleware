using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;

namespace RequestMonitoring.Library.Middleware.Services.DomainCache;

/// <summary>
/// Сервис для управления кэшем доменов
/// </summary>
public class DomainCacheService(IDistributedCache cache, DomainListsContext context, ILogger<DomainCacheService> logger) : IDomainCacheService
{
    /// <summary>
    /// Удаляет из кэша конкретный домен
    /// </summary>
    public async Task InvalidateDomainAsync(string host)
    {
        try
        {
            var cacheKey = $"Domain_{host}";
            await cache.RemoveAsync(cacheKey);
            logger.LogInformation("Cache invalidated for domain: {Host}", host);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache for domain: {Host}", host);
        }
    }

    /// <summary>
    /// Удаляет все записи доменов из кэша
    /// </summary>
    public async Task InvalidateAllDomainsAsync()
    {
        try
        {
            var allDomains = await context.Domains
                .Select(d => d.Host)
                .ToListAsync();

            var tasks = allDomains.Select(host => 
            {
                var cacheKey = $"Domain_{host}";
                return cache.RemoveAsync(cacheKey);
            });

            await Task.WhenAll(tasks);
            
            logger.LogInformation("Cache invalidated for all {Count} domains", allDomains.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache for all domains");
        }
    }
}
