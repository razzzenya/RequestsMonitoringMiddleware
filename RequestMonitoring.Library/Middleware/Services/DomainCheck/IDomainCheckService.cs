using Microsoft.AspNetCore.Http;
using RequestMonitoring.Library.Enitites.Domain;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Интерфейс сервиса проверки статуса домена
/// </summary>
public interface IDomainCheckService
{
    /// <summary>
    /// Проверяет статус домена из контекста запроса
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса</param>
    /// <returns>Статус домена</returns>
    Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context);
}
