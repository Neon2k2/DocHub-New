using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("ExcelUploads")]
public class ExcelUpload
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public Guid FileId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? ParsedData { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? FieldMappings { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? ProcessingOptions { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? Results { get; set; } // JSON

    public bool IsProcessed { get; set; } = false;

    public int ProcessedRows { get; set; } = 0;

    [Required]
    public Guid ProcessedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("LetterTypeDefinitionId")]
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;

    [ForeignKey("FileId")]
    public virtual FileReference File { get; set; } = null!;

    [ForeignKey("ProcessedBy")]
    public virtual User ProcessedByUser { get; set; } = null!;
}
