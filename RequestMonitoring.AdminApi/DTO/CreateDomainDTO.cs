namespace RequestMonitoring.AdminApi.DTO;

public class CreateDomainDTO
{
    public required string Host { get; set; }
    public required int DomainStatusTypeId { get; set; }
}
