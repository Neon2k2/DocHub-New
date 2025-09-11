using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class LetterTypeDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string TypeKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataSourceType { get; set; } = "Database"; // Database or Excel
    
    public string? FieldConfiguration { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public Guid? ModuleId { get; set; }
    
    // Navigation properties
    public virtual Module? Module { get; set; }
    public virtual ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
    public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
    public virtual ICollection<ExcelUpload> ExcelUploads { get; set; } = new List<ExcelUpload>();
}
