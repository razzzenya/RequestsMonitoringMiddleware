using RequestsMonitoringMiddleware.Domain.Enums;

namespace RequestsMonitoringMiddleware.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public AlertType Type { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
}
