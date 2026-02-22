using Microsoft.Extensions.Configuration;
using OpenSearch.Client;
using RequestMonitoring.Library.Enitites.Domain;

namespace RequestMonitoring.Library.Middleware.Services.OpenSearchLog;

/// <summary>
/// Сервис для работы с логами в OpenSearch
/// </summary>
public class OpenSearchLogService(IOpenSearchClient client, IConfiguration configuration) : IOpenSearchLogService
{
    private readonly IOpenSearchClient client = client;
    private readonly string index = configuration["OpenSearch:Index"] ?? "request-logs";

    /// <summary>
    /// Индексирует лог запроса в OpenSearch
    /// </summary>
    /// <param name="log">Лог запроса для индексации</param>
    public async Task IndexAsync(RequestLog log)
    {
        await client.IndexAsync(log, i => i.Index(index).Id(log.Id));
    }

    /// <summary>
    /// Выполняет поиск логов в OpenSearch
    /// </summary>
    public async Task<List<RequestLog>> SearchAsync(string query, int size = 50)
    {
        var resp = await client.SearchAsync<RequestLog>(s => s
            .Index(index)
            .Query(q => q
                .QueryString(qs => qs.Query(query ?? "*"))
            )
            .Size(size)
            .Sort(ss => ss.Descending(p => p.TimestampUtc))
        );

        var results = resp.Hits.Select(h => h.Source).Where(s => s != null).Cast<RequestLog>().ToList();
        return results;
    }
}
