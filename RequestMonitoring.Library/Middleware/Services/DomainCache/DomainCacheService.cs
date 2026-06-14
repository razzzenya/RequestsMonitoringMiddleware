using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;

namespace RequestMonitoring.Library.Middleware.Services.DomainCache;

/// <summary>
/// Сервис для управления кэшем доменов
/// </summary>
public class DomainCacheService(HybridCache cache, DomainListsContext context, ILogger<DomainCacheService> logger) : IDomainCacheService
{
    /// <summary>
    /// Удаляет из кэша конкретный домен
    /// </summary>
    public async Task InvalidateDomainAsync(string host)
    {
        try
        {
            await cache.RemoveAsync($"Domain_{host}");
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

            await Task.WhenAll(allDomains.Select(host => cache.RemoveAsync($"Domain_{host}").AsTask()));

            logger.LogInformation("Cache invalidated for all {Count} domains", allDomains.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache for all domains");
        }
    }
}
