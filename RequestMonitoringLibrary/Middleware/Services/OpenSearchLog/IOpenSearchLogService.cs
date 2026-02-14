using RequestMonitoring.Library.Enitites.Domain;

namespace RequestMonitoring.Library.Middleware.Services.OpenSearchLog;

/// <summary>
/// Интерфейс сервиса для работы с логами в OpenSearch
/// </summary>
public interface IOpenSearchLogService
{
    /// <summary>
    /// Индексирует лог запроса в OpenSearch
    /// </summary>
    /// <param name="log">Лог запроса для индексации</param>
    Task IndexAsync(RequestLog log);
    
    /// <summary>
    /// Выполняет поиск логов в OpenSearch
    /// </summary>
    /// <param name="query">Поисковый запрос</param>
    /// <param name="size">Максимальное количество результатов</param>
    /// <returns>Список найденных логов</returns>
    Task<List<RequestLog>> SearchAsync(string query, int size = 50);
}
