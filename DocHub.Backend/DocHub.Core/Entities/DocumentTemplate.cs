using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("DocumentTemplates")]
public class DocumentTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public Guid FileId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Placeholders { get; set; } // JSON

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("FileId")]
    public virtual FileReference File { get; set; } = null!;

    public virtual ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
}
