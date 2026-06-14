using RequestMonitoring.Library.Dto;

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

/// <summary>
/// Сервис проверки и инкремента квоты домена
/// </summary>
public interface IQuotaCheckService
{
    /// <summary>
    /// Проверяет квоту домена и инкрементирует счётчик.
    /// Принимает метаданные квоты из кэша — при null возвращает NoQuota.
    /// </summary>
    Task<QuotaCheckResult> CheckAndIncrementAsync(string host, QuotaMetaDto? quotaMeta);
}
