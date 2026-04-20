namespace RequestMonitoring.AdminApi.DTO;

/// <summary>
/// Данные для создания или обновления домена
/// </summary>
/// <param name="Host">Хост домена</param>
/// <param name="DomainStatusTypeId">Идентификатор типа статуса</param>
public record DomainCreateUpdateDto(string Host, int DomainStatusTypeId);
