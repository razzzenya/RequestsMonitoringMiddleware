using Microsoft.Extensions.Configuration;
using OpenSearch.Client;
using RequestMonitoringLibrary.Enitites.Domain;

namespace RequestMonitoringLibrary.Middleware.Services.OpenSearchLog;

public class OpenSearchLogService : IOpenSearchLogService
{
    private readonly OpenSearchClient client;
    private readonly string index;

    public OpenSearchLogService(IConfiguration configuration)
    {
        var uriString = configuration["OpenSearch:Uri"] ?? "http://localhost:9200";
        index = configuration["OpenSearch:Index"] ?? "request-logs";
        var uri = new Uri(uriString);

        var settings = new ConnectionSettings(uri)
            .DefaultIndex(index)
            .ThrowExceptions();

        client = new OpenSearchClient(settings);

        var existsResp = client.Indices.Exists(index);
        if (!existsResp.Exists)
        {
            client.Indices.Create(index, c => c
                .Map<RequestLog>(m => m
                    .AutoMap()
                )
            );
        }
    }

    public async Task IndexAsync(RequestLog log)
    {
        await client.IndexAsync(log, i => i.Index(index).Id(log.Id));
    }

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
