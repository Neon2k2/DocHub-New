using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class FileStorage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? SubCategory { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UploadedBy { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsTemporary { get; set; } = false;
    
    public string? Metadata { get; set; }
}
