using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class GeneratedDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [Required]
    public Guid EmployeeId { get; set; }
    
    public Guid? TemplateId { get; set; }
    
    public Guid? SignatureId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [MaxLength(1000)]
    public string? DownloadUrl { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string GeneratedBy { get; set; } = string.Empty;
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public string? Metadata { get; set; }
    
    // Foreign keys
    public Guid? ModuleId { get; set; }
    
    // Navigation properties
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
    public virtual Module? Module { get; set; }
}
