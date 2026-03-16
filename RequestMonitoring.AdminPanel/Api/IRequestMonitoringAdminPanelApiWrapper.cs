namespace RequestMonitoring.AdminPanel.Api;

public interface IRequestMonitoringAdminPanelApiWrapper
{
    public Task<IEnumerable<Domain>> GetDomainList();
    public Task<Domain> GetDomain(int id);
    public Task<Domain> CreateDomain(CreateDomainDTO dto);
    public Task<Domain> UpdateDomain(int id, UpdateDomainDTO dto);
    public Task DeleteDomain(int id);
}
