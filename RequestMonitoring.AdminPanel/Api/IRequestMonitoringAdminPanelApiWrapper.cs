namespace RequestMonitoring.AdminPanel.Api;

public interface IRequestMonitoringAdminPanelApiWrapper
{
    public Task<IEnumerable<DomainDto>> GetDomainList();
    public Task<DomainDto> GetDomain(int id);
    public Task<DomainDto> CreateDomain(CreateUpdateDomainDto dto);
    public Task<DomainDto> UpdateDomain(int id, CreateUpdateDomainDto dto);
    public Task DeleteDomain(int id);
    public Task<IEnumerable<DomainStatusTypeDto>> GetDomainStatusTypes();
    public Task<IEnumerable<QuotaDto>> GetQuotaList();
    public Task<QuotaDto> GetQuota(int id);
    public Task<QuotaDto> GetQuotaByDomainId(int id);
    public Task<QuotaDto> CreateQuota(CreateQuotaDto quota);
    public Task<QuotaDto> UpdateQuota(int id, UpdateQuotaDto quota);
    public Task DeleteQuota(int id);
    public Task<QuotaDto> ResetCounter(int id);
}
