namespace RequestMonitoring.AdminPanel.Api;

public class RequestMonitoringAdminPanelApiWrapper(IConfiguration configuration) : IRequestMonitoringAdminPanelApiWrapper
{
    public readonly RequestMonitoringAdminPanelApi _client = new(configuration["OpenApi:ServerUrl"], new HttpClient());

    public async Task<Domain> CreateDomain(CreateDomainDTO dto) => await _client.DomainsPOSTAsync(dto);

    public async Task DeleteDomain(int id) => await _client.DomainsDELETEAsync(id);

    public async Task<Domain> GetDomain(int id) => await _client.DomainsGETAsync(id);

    public async Task<IEnumerable<Domain>> GetDomainList() => await _client.DomainsAllAsync();

    public async Task<Domain> UpdateDomain(int id, UpdateDomainDTO dto) => await _client.DomainsPUTAsync(id, dto);
}
