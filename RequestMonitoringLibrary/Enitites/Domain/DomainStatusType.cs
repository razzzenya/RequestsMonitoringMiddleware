using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequestMonitoringLibrary.Enitites.Domain;

public class DomainStatusType
{
    [Key]
    [Column("id")]
    public required int Id { get; set; }

    [Column("name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
}
