namespace RequestMonitoring.AdminApi.DTO;

public class UpdateDomainDTO
{
    public required string Host { get; init; }
    public required int DomainStatusTypeId { get; init; }
}
