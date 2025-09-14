using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("DynamicFields")]
public class DynamicField
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string FieldType { get; set; } = string.Empty; // "text", "number", "date", "email", "dropdown", etc.

    public bool IsRequired { get; set; } = false;

    [Column(TypeName = "nvarchar(max)")]
    public string? ValidationRules { get; set; } // JSON

    [MaxLength(255)]
    public string? DefaultValue { get; set; }

    public int OrderIndex { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("LetterTypeDefinitionId")]
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
}
