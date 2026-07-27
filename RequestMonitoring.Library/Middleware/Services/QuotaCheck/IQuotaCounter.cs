namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

/// <summary>
/// Абстракция над runtime-счётчиком квоты
/// Все операции работают только с Redis, не обращаясь к БД на hot path
/// </summary>
public interface IQuotaCounter
{
    /// <summary>
    /// Инкрементирует счётчик total-квоты. Если ключ отсутствует, засеивает его значением из БД
    /// </summary>
    Task<long> IncrementTotalAsync(int domainId, long seedRequestCount);

    /// <summary>
    /// Проверяет/сбрасывает период и инкрементирует счётчик periodic-квоты
    /// Если ключ отсутствует, засеивает его значением из БД
    /// </summary>
    Task<long> IncrementPeriodicAsync(int domainId, int periodSeconds, long seedRequestCount);

    /// <summary>
    /// Удаляет все runtime-ключи квоты домена
    /// </summary>
    Task DeleteAsync(int domainId);
}
