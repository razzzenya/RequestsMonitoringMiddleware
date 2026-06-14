using Microsoft.AspNetCore.Http;
using RequestMonitoring.Library.Dto;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Сервис проверки статуса домена
/// </summary>
public interface IDomainCheckService
{
    /// <summary>
    /// Возвращает статус домена и метаданные квоты из кэша или БД
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса</param>
    /// <returns>Статус домена и метаданные</returns>
    Task<DomainCacheEntry> IsDomainAllowedAsync(HttpContext context);
}
