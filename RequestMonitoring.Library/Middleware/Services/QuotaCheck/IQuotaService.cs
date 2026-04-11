namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

public enum QuotaCheckResult
{
    /// <summary>
    /// Квоты нет - пропускаем без ограничений
    /// </summary>
    NoQuota,

    /// <summary>
    /// Квота не превышена - запрос разрешён
    /// </summary>
    Allowed,

    /// <summary>
    /// Квота исчерпана или истекла - домен переводится в Greylisted
    /// </summary>
    Exceeded,

    /// <summary>
    /// Периодическая квота исчерпана - домен не блокируется, сбросится по истечению периода
    /// </summary>
    TemporarilyExceeded
}

public interface IQuotaService
{
    /// <summary>
    /// Проверяет квоту домена и инкрементирует счётчик при успехе
    /// </summary>
    Task<QuotaCheckResult> CheckAndIncrementAsync(string host);
}
