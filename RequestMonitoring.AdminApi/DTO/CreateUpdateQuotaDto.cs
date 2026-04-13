using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.AdminApi.DTO;

public record CreateUpdateQuotaDto(int DomainId, QuotaType Type, int? MaxRequests, int? PeriodSeconds, DateTime? ExpiresAt);
