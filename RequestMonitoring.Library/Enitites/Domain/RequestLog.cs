namespace RequestMonitoring.Library.Enitites.Domain;

/// <summary>
/// Модель лога HTTP-запроса
/// </summary>
public class RequestLog
{
    /// <summary>
    /// Уникальный идентификатор лога
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Временная метка запроса в UTC
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// HTTP-метод запроса (GET, POST, и т.д.)
    /// </summary>
    public string Method { get; set; } = "";
    
    /// <summary>
    /// Путь запроса
    /// </summary>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// Строка параметров запроса
    /// </summary>
    public string QueryString { get; set; } = "";
    
    /// <summary>
    /// Заголовки HTTP-запроса
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
    
    /// <summary>
    /// IP-адрес клиента
    /// </summary>
    public string RemoteIp { get; set; } = "";
    
    /// <summary>
    /// HTTP-код ответа
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// Длительность обработки запроса в миллисекундах
    /// </summary>
    public long DurationMs { get; set; }
    
    /// <summary>
    /// Предварительный просмотр тела запроса
    /// </summary>
    public string? BodyPreview { get; set; }
}
