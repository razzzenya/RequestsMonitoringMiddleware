using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.AdminApi.DTO;

/// <summary>
/// Данные для создания или обновления квоты домена
/// </summary>
/// <param name="DomainId">Идентификатор домена (используется только при создании)</param>
/// <param name="Type">Тип квоты</param>
/// <param name="MaxRequests">Максимальное количество запросов, null - безлимит</param>
/// <param name="PeriodSeconds">Период сброса счётчика в секундах, null - счётчик не сбрасывается</param>
/// <param name="ExpiresAt">Дата истечения квоты, null - бессрочно</param>
public record QuotaCreateUpdateDto(int? DomainId, QuotaType Type, int? MaxRequests, int? PeriodSeconds, DateTime? ExpiresAt);
