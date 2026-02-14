using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequestMonitoring.Library.Enitites.Domain;

/// <summary>
/// Сущность, представляющая домен
/// </summary>
[Table("domain")]
public class Domain
{
    /// <summary>
    /// Идентификатор домена
    /// </summary>
    [Key]
    [Column("id")]
    [Required]
    public required int Id { get; set; }
    /// <summary>
    /// Хост домена
    /// </summary>
    [Column("host")]
    [Required]
    public required string Host { get; set; } = "";
    /// <summary>
    /// Статус домена
    /// </summary>
    [Column("status")]
    [Required]
    public required DomainStatusType DomainStatusType { get; set; }
    /// <summary>
    /// Идентификатор типа статуса домена
    /// </summary>
    [Column("status_id")]
    [Required]
    public required int DomainStatusTypeId { get; set; }
}
