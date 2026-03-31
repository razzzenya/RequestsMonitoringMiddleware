namespace RequestMonitoring.AdminApi.DTO;

/// <summary>
/// Данные домена
/// </summary>
/// <param name="Id">Идентификатор домена</param>
/// <param name="Host">Хост домена</param>
/// <param name="DomainStatusTypeId">Идентификатор типа статуса</param>
/// <param name="DomainStatusName">Название статуса</param>
public record DomainDto(int Id, string Host, int DomainStatusTypeId, string DomainStatusName);
