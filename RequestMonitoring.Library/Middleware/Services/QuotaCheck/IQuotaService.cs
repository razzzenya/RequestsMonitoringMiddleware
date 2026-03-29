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
    /// Квота исчерпана или истекла
    /// </summary>
    Exceeded
}

public interface IQuotaService
{
    /// <summary>
    /// Проверяет квоту домена и инкрементирует счётчик при успехе
    /// </summary>
    Task<QuotaCheckResult> CheckAndIncrementAsync(string host);
}
