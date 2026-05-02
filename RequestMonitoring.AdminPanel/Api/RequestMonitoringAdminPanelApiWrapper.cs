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
        httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        return new RequestMonitoringAdminPanelApi(httpClient)
        {
            ReadResponseAsString = true
        };
    }

    public async Task<bool> LoginAsync(string login, string password)
    {
        try
        {
            await _client.LoginAsync(new LoginDto { Login = login, Password = password });
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _client.LogoutAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 200)
        {
        }
    }

    public async Task<DomainDto> CreateDomain(DomainCreateUpdateDto dto) => await _client.DomainsPOSTAsync(dto);

    public async Task<QuotaDto> CreateQuota(QuotaCreateUpdateDto quota) => await _client.QuotasPOSTAsync(quota);

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

    public async Task<IReadOnlyList<DomainDto>> GetDomainList() => (IReadOnlyList<DomainDto>)await _client.DomainsAllAsync();

    public async Task<PagedResultOfDomainDto> GetDomainListPagedAsync(int page, int pageSize, string? search = null)
        => await _client.GetDomainsPagedAsync(page, pageSize, search);

    public async Task<PagedResultOfQuotaDto> GetQuotaListPagedAsync(int page, int pageSize, int? domainId = null)
        => await _client.GetQuotasPagedAsync(page, pageSize, domainId);

    public async Task<IReadOnlyList<DomainStatusTypeDto>> GetDomainStatusTypes() => (IReadOnlyList<DomainStatusTypeDto>)await _client.DomainStatusTypesAsync();

    public async Task<QuotaDto> GetQuota(int id) => await _client.GetQuotaByIdAsync(id);

    public Task<QuotaDto> GetQuotaByDomainId(int id) => _client.GetQuotaByDomainIdAsync(id);

    public async Task<IReadOnlyList<QuotaDto>> GetQuotaList() => (IReadOnlyList<QuotaDto>)await _client.QuotasAllAsync();

    public async Task<QuotaDto> ResetCounter(int id) => await _client.ResetCounterAsync(id);

    public async Task<DomainDto> UpdateDomain(int id, DomainCreateUpdateDto dto) => await _client.DomainsPUTAsync(id, dto);

    public async Task<QuotaDto> UpdateQuota(int id, QuotaCreateUpdateDto quota) => await _client.QuotasPUTAsync(id, quota);
}
