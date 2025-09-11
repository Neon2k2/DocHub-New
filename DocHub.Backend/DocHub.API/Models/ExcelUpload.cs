using System.ComponentModel.DataAnnotations;

namespace DocHub.API.Models;

/// <summary>
/// Represents an uploaded Excel file for a specific tab
/// </summary>
public class ExcelUpload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; } // FK to LetterTypeDefinition (keeping old name for compatibility)
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    
    public long FileSize { get; set; }
    
    [MaxLength(100)]
    public string UploadedBy { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// JSON field storing metadata about the upload
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// JSON field storing the parsed Excel data
    /// </summary>
    public string? ParsedData { get; set; }
    
    /// <summary>
    /// JSON field storing column mappings
    /// Example: {"employee_id": "A", "name": "B", "department": "C"}
    /// </summary>
    public string? FieldMappings { get; set; }
    
    /// <summary>
    /// JSON field storing processing options
    /// </summary>
    public string? ProcessingOptions { get; set; }
    
    /// <summary>
    /// Whether the Excel file has been processed and data imported
    /// </summary>
    public bool IsProcessed { get; set; } = false;
    
    /// <summary>
    /// Number of rows processed from the Excel file
    /// </summary>
    public int ProcessedRows { get; set; } = 0;
    
    [MaxLength(100)]
    public string? ProcessedBy { get; set; }
    
    /// <summary>
    /// JSON field storing processing results
    /// </summary>
    public string? Results { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public LetterTypeDefinition? LetterTypeDefinition { get; set; }
}