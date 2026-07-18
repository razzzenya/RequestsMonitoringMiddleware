namespace RequestMonitoring.Library.Middleware.Services.QuotaCache;

/// <summary>
/// Сервис для управления кэшем runtime-состояния квот
/// </summary>
public interface IQuotaCacheService
{
    /// <summary>
    /// Удаляет runtime-состояние квоты домена из Redis (счётчик и lastReset)
    /// </summary>
    Task InvalidateQuotaAsync(int domainId);
}
