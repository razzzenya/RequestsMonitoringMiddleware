namespace RequestsMonitoringMiddleware.Domain.Entities;

public class Quota
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid CoverageId { get; set; }
    public Coverage Coverage { get; set; } = null!;
    public int MaxRequestsPerDay { get; set; }
    public long MaxBytesPerDay { get; set; }
    public int CurrentRequests { get; set; }
    public long CurrentBytes { get; set; }
    public DateTime LastReset { get; set; }
}