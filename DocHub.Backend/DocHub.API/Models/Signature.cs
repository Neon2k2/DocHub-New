using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class Signature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? FileUrl { get; set; }
    
    public long FileSize { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public Guid? ModuleId { get; set; }
    
    // Navigation properties
    public virtual Module? Module { get; set; }
}
