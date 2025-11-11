namespace RequestsMonitoringMiddleware.Domain.Entities;

public class RequestLog
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid CoverageId { get; set; }
    public DateTime Timestamp { get; set; }
    public long BytesSent { get; set; }
    public bool Allowed { get; set; }
    public string IpAddress { get; set; } = null!;
}