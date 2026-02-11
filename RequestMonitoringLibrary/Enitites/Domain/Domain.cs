using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequestMonitoringLibrary.Enitites.Domain;

public class Domain
{
    [Key]
    [Column("id")]
    public required int Id { get; set; }
    [Column("host")]
    [Required]
    public required string Host { get; set; } = "";
    [Column("status")]
    [Required]
    public required DomainStatusType DomainStatusType { get; set; }
    [Column("status_id")]
    [Required]
    public required int DomainStatusTypeId { get; set; }
}
