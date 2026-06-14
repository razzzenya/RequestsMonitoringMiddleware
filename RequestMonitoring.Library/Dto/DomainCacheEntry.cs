namespace RequestMonitoring.Library.Dto;

/// <summary>
/// Единая запись кэша домена: статус + метаданные квоты
/// </summary>
public record DomainCacheEntry(
    DomainStatus Status,
    QuotaMetaDto? Quota
);
