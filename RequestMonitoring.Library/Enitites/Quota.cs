using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.Library.Enitites;

[Table("quota")]
public class Quota
{
    [Key]
    [Column("id")]
    public required int Id { get; set; }

    [Column("domain_id")]
    [Required]
    public required int DomainId { get; set; }

    public required Domain Domain { get; set; }

    /// <summary>
    /// Тип квоты
    /// </summary>
    [Column("type")]
    [Required]
    public required QuotaType Type { get; set; }

    /// <summary>
    /// Макс. кол-во запросов, null = безлимит
    /// </summary>
    [Column("max_requests")]
    public int? MaxRequests { get; set; }

    /// <summary>
    /// Период сброса счётчика в секундах, null = счётчик не сбрасывается
    /// </summary>
    [Column("period_seconds")]
    public int? PeriodSeconds { get; set; }

    /// <summary>
    /// Дата истечения квоты, null = бессрочно
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Текущий счётчик запросов
    /// </summary>
    [Column("request_count")]
    public long RequestCount { get; set; }

    /// <summary>
    /// Время последнего сброса счётчика
    /// </summary>
    [Column("last_reset_at")]
    public DateTime? LastResetAt { get; set; }
}
