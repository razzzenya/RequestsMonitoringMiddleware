namespace RequestMonitoring.AdminPanel.Api;

public interface IRequestMonitoringAdminPanelApiWrapper
{
    public Task<IEnumerable<DomainDto>> GetDomainList();
    public Task<DomainDto> GetDomain(int id);
    public Task<DomainDto> CreateDomain(CreateUpdateDomainDto dto);
    public Task<DomainDto> UpdateDomain(int id, CreateUpdateDomainDto dto);
    public Task DeleteDomain(int id);
}
