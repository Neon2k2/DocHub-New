using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; } // JSON

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
