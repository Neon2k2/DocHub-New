using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("LetterTypeDefinitions")]
public class LetterTypeDefinition
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string TypeKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string DataSourceType { get; set; } = string.Empty; // "Database" or "Excel"

    [Required]
    [MaxLength(20)]
    public string Department { get; set; } = string.Empty; // "ER" or "Billing"

    [Column(TypeName = "nvarchar(max)")]
    public string? FieldConfiguration { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? TableSchema { get; set; } // JSON

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<DynamicField> DynamicFields { get; set; } = new List<DynamicField>();
    public virtual ICollection<ExcelUpload> ExcelUploads { get; set; } = new List<ExcelUpload>();
    public virtual ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
    public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
}
