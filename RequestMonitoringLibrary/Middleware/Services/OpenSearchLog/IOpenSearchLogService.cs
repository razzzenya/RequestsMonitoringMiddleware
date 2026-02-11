using RequestMonitoringLibrary.Enitites.Domain;

namespace RequestMonitoringLibrary.Middleware.Services.OpenSearchLog;

public interface IOpenSearchLogService
{
    Task IndexAsync(RequestLog log);
    Task<List<RequestLog>> SearchAsync(string query, int size = 50);
}
