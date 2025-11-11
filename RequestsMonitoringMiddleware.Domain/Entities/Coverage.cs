namespace RequestsMonitoringMiddleware.Domain.Entities;

public class Coverage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string SourceUrl { get; set; } = null!;
    public ICollection<Quota> Quotas { get; set; } = new List<Quota>();
}
