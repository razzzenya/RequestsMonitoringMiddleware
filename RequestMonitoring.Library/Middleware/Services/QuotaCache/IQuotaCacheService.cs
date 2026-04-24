namespace RequestMonitoring.Library.Middleware.Services.QuotaCache;

/// <summary>
/// Сервис для управления кэшем счётчиков квот
/// </summary>
public interface IQuotaCacheService
{
    /// <summary>
    /// Удаляет счётчик квоты из кэша по идентификатору домена
    /// </summary>
    Task InvalidateQuotaAsync(int domainId);
}