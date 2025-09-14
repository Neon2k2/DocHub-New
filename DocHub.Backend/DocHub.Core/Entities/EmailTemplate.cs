using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("EmailTemplates")]
public class EmailTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Placeholders { get; set; } // JSON

    public bool IsActive { get; set; } = true;

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
}
