using System.ComponentModel.DataAnnotations;

namespace DocHub.API.Models;

public class DocumentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid EmployeeId { get; set; }
    
    [Required]
    public Guid DocumentTemplateId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    public Guid RequestedBy { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, InProgress
    
    public string? Metadata { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public Guid? ApprovedBy { get; set; }
    
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    
    public DateTime? EffectiveDate { get; set; }
    
    public Guid? GeneratedDocumentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual DocumentTemplate? DocumentTemplate { get; set; }
    public virtual User? RequestedByUser { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual GeneratedDocument? GeneratedDocument { get; set; }
}
