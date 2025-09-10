using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class EmailJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EmployeeId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string EmployeeEmail { get; set; } = string.Empty;
    
    public Guid? DocumentId { get; set; }
    
    public Guid? EmailTemplateId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? Attachments { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    [Required]
    [MaxLength(100)]
    public string SentBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentAt { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public DateTime? OpenedAt { get; set; }
    
    public DateTime? ClickedAt { get; set; }
    
    public DateTime? BouncedAt { get; set; }
    
    public DateTime? DroppedAt { get; set; }
    
    public DateTime? SpamReportedAt { get; set; }
    
    public DateTime? UnsubscribedAt { get; set; }
    
    [MaxLength(500)]
    public string? BounceReason { get; set; }
    
    [MaxLength(500)]
    public string? DropReason { get; set; }
    
    [MaxLength(100)]
    public string? TrackingId { get; set; }
    
    [MaxLength(100)]
    public string? SendGridMessageId { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? LastRetryAt { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public Guid? ModuleId { get; set; }
    
    // Navigation properties
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;
    public virtual Module? Module { get; set; }
    public virtual ICollection<EmailEvent> Events { get; set; } = new List<EmailEvent>();
}

public class EmailEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid EmailJobId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(1000)]
    public string? Url { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public string? Data { get; set; }
    
    // Navigation properties
    public virtual EmailJob EmailJob { get; set; } = null!;
}
