using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.Library.Dto;

/// <summary>
/// Кэшируемые метаданные квоты, не включает LastResetAt и RequestCount
/// </summary>
public record QuotaMetaDto(
    int Id,
    int DomainId,
    QuotaType Type,
    int? MaxRequests,
    int? PeriodSeconds,
    DateTime? ExpiresAt
);
