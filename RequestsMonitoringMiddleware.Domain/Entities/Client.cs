using RequestsMonitoringMiddleware.Domain.Enums;

namespace RequestsMonitoringMiddleware.Domain.Entities;

public class Client
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ClientStatus Status { get; set; } = ClientStatus.Whitelisted;
    public DateTime? LastPaymentDate { get; set; }
    public ICollection<Quota> Quotas { get; set; } = [];
}
