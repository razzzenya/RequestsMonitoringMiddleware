namespace RequestMonitoring.AdminApi.DTO;

/// <summary>
/// Тип статуса домена
/// </summary>
/// <param name="Id">Идентификатор типа статуса</param>
/// <param name="Name">Название статуса</param>
public record DomainStatusTypeDto(int Id, string Name);
