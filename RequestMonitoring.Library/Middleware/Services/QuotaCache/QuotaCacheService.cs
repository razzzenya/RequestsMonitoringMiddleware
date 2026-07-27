using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCache;

/// <summary>
/// Сервис для управления кэшем runtime-состояния квот
/// </summary>
public class QuotaCacheService(IQuotaCounter quotaCounter, ILogger<QuotaCacheService> logger) : IQuotaCacheService
{
    /// <summary>
    /// Удаляет runtime-состояние квоты домена из Redis (счётчик и lastReset)
    /// </summary>
    public async Task InvalidateQuotaAsync(int domainId)
    {
        try
        {
            await quotaCounter.DeleteAsync(domainId);
            logger.LogInformation("Quota cache invalidated for domain ID: {DomainId}", domainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate quota cache for domain ID: {DomainId}", domainId);
        }
    }
}
