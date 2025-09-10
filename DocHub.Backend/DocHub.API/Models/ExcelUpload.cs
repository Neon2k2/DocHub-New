using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class ExcelUpload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public int ProcessedRows { get; set; }
    
    public int ValidRows { get; set; }
    
    public int InvalidRows { get; set; }
    
    public string? FieldMappings { get; set; }
    
    public string? ProcessingOptions { get; set; }
    
    public string? Results { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProcessedBy { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string UploadedBy { get; set; } = string.Empty;
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public string? Metadata { get; set; }
    
    // Navigation properties
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
}

public class FieldMapping
{
    public string ExcelColumn { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string FieldType { get; set; } = "Text";
    public bool IsRequired { get; set; } = false;
}
