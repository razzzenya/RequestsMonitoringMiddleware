using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.AdminApi.DTO;

public record QuotaDto(int Id, int DomainId, QuotaType Type, int? MaxRequests,
    int? PeriodSeconds, DateTime? ExpiresAt, long RequestCount, DateTime? LastResetAt);
