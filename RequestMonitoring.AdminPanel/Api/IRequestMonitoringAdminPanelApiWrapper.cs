namespace RequestMonitoring.AdminPanel.Api;

public interface IRequestMonitoringAdminPanelApiWrapper
{
    public Task<IReadOnlyList<DomainDto>> GetDomainList();
    public Task<DomainDto> GetDomain(int id);
    public Task<DomainDto> CreateDomain(DomainCreateUpdateDto dto);
    public Task<DomainDto> UpdateDomain(int id, DomainCreateUpdateDto dto);
    public Task DeleteDomain(int id);
    public Task<IReadOnlyList<DomainStatusTypeDto>> GetDomainStatusTypes();
    public Task<IReadOnlyList<QuotaDto>> GetQuotaList();
    public Task<QuotaDto> GetQuota(int id);
    public Task<QuotaDto> GetQuotaByDomainId(int id);
    public Task<QuotaDto> CreateQuota(QuotaCreateUpdateDto quota);
    public Task<QuotaDto> UpdateQuota(int id, QuotaCreateUpdateDto quota);
    public Task DeleteQuota(int id);
    public Task<QuotaDto> ResetCounter(int id);
}
