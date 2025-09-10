using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class DynamicFieldData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid EmployeeId { get; set; }
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;
    
    [Required]
    public string FieldValue { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
}
