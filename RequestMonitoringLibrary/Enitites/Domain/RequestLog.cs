namespace RequestMonitoringLibrary.Enitites.Domain;

public class RequestLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string QueryString { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string RemoteIp { get; set; } = "";
    public int? StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? BodyPreview { get; set; }
}
