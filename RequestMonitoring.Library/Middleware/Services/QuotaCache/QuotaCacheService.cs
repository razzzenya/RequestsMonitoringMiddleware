using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCache;

/// <summary>
/// Сервис для управления кэшем счётчиков квот
/// </summary>
public class QuotaCacheService(IDistributedCache cache, ILogger<QuotaCacheService> logger) : IQuotaCacheService
{
    /// <summary>
    /// Удаляет счётчик квоты из кэша по идентификатору домена
    /// </summary>
    public async Task InvalidateQuotaAsync(int domainId)
    {
        try
        {
            var cacheKey = $"Quota_{domainId}";
            await cache.RemoveAsync(cacheKey);
            logger.LogInformation("Quota cache invalidated for domain ID: {DomainId}", domainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate quota cache for domain ID: {DomainId}", domainId);
        }
    }
}