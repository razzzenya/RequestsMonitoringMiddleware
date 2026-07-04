using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.AdminApi.DTO;
/// <summary>
/// не забыть указать
/// </summary>
/// <param name="Id"></param>
/// <param name="DomainId"></param>
/// <param name="Type"></param>
/// <param name="MaxRequests"></param>
/// <param name="PeriodSeconds"></param>
/// <param name="ExpiresAt"></param>
/// <param name="RequestCount"></param>
/// <param name="LastResetAt"></param>
public record QuotaDto(int Id, int DomainId, QuotaType Type, int? MaxRequests,
    int? PeriodSeconds, DateTime? ExpiresAt, long RequestCount, DateTime? LastResetAt);
