namespace RequestMonitoring.AdminPanel.Api;

public class RequestMonitoringAdminPanelApiWrapper(IConfiguration configuration, HttpClient httpClient) : IRequestMonitoringAdminPanelApiWrapper
{
    private readonly RequestMonitoringAdminPanelApi _client = CreateClient(configuration, httpClient);

    private static RequestMonitoringAdminPanelApi CreateClient(IConfiguration configuration, HttpClient httpClient)
    {
        var baseUrl = configuration["OpenApi:ServerUrl"]
            ?? configuration["services:adminapi:https:0"]
            ?? configuration["services:adminapi:http:0"]
            ?? "https://localhost:7213";

        var client = new RequestMonitoringAdminPanelApi(baseUrl, httpClient)
        {
            ReadResponseAsString = true
        };
        return client;
    }

    public async Task<Domain> CreateDomain(CreateDomainDTO dto) => await _client.DomainsPOSTAsync(dto);

    public async Task DeleteDomain(int id)
    {
        try
        {
            await _client.DomainsDELETEAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 204)
        {
            return;
        }
    }

    public async Task<Domain> GetDomain(int id) => await _client.DomainsGETAsync(id);

    public async Task<IEnumerable<Domain>> GetDomainList() => await _client.DomainsAllAsync();

    public async Task<Domain> UpdateDomain(int id, UpdateDomainDTO dto) => await _client.DomainsPUTAsync(id, dto);
}
