using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class DynamicField
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text";
    
    public bool IsRequired { get; set; } = false;
    
    public string? ValidationRules { get; set; }
    
    [MaxLength(500)]
    public string? DefaultValue { get; set; }
    
    public int Order { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
    public virtual ICollection<DynamicFieldData> FieldData { get; set; } = new List<DynamicFieldData>();
}

public enum FieldType
{
    Text,
    Number,
    Date,
    Email,
    PhoneNumber,
    Currency,
    Percentage,
    Boolean,
    Dropdown,
    TextArea,
    Url,
    Image,
    File,
    DateTime,
    Time,
    Json
}
