using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequestMonitoring.Library.Enitites.Domain;

/// <summary>
/// Тип статуса домена
/// </summary>
public class DomainStatusType
{
    /// <summary>
    /// Идентификатор типа статуса
    /// </summary>
    [Key]
    [Column("id")]
    public required int Id { get; set; }

    /// <summary>
    /// Название статуса
    /// </summary>
    [Column("name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
}
