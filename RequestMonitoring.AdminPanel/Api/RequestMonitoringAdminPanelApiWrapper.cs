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

        httpClient.BaseAddress = new Uri(baseUrl);

        return new RequestMonitoringAdminPanelApi(httpClient)
        {
            ReadResponseAsString = true
        };
    }

    public async Task<DomainDto> CreateDomain(CreateUpdateDomainDto dto) => await _client.DomainsPOSTAsync(dto);

    public async Task<QuotaDto> CreateQuota(CreateQuotaDto quota) => await _client.QuotasPOSTAsync(quota);

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

    public async Task DeleteQuota(int id)
    {
        try
        {
            await _client.QuotasDELETEAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 204)
        {
            return;
        }
    }

    public async Task<DomainDto> GetDomain(int id) => await _client.DomainsGETAsync(id);

    public async Task<IEnumerable<DomainDto>> GetDomainList() => await _client.DomainsAllAsync();

    public async Task<IEnumerable<DomainStatusTypeDto>> GetDomainStatusTypes() => await _client.DomainStatusTypesAsync();
    public async Task<QuotaDto> GetQuota(int id) => await _client.GetQuotaByIdAsync(id);

    public Task<QuotaDto> GetQuotaByDomainId(int id) => _client.GetQuotaByDomainIdAsync(id);

    public async Task<IEnumerable<QuotaDto>> GetQuotaList() => await _client.QuotasAllAsync();

    public async Task<QuotaDto> ResetCounter(int id) => await _client.ResetCounterAsync(id);

    public async Task<DomainDto> UpdateDomain(int id, CreateUpdateDomainDto dto) => await _client.DomainsPUTAsync(id, dto);

    public async Task<QuotaDto> UpdateQuota(int id, UpdateQuotaDto quota) => await _client.QuotasPUTAsync(id, quota);
}
