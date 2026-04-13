using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.AdminApi.DTO;

/// <summary>
/// Данные для обновления квоты домена
/// </summary>
/// <param name="Type">Тип квоты</param>
/// <param name="MaxRequests">Максимальное количество запросов, null - безлимит</param>
/// <param name="PeriodSeconds">Период сброса счётчика в секундах, null - счётчик не сбрасывается</param>
/// <param name="ExpiresAt">Дата истечения квоты, null - бессрочно</param>
public record UpdateQuotaDto(QuotaType Type, int? MaxRequests, int? PeriodSeconds, DateTime? ExpiresAt);