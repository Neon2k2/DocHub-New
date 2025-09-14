using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("EmailJobs")]
public class EmailJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public Guid ExcelUploadId { get; set; }

    public Guid? DocumentId { get; set; }

    public Guid? EmailTemplateId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Attachments { get; set; } // JSON

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, sent, delivered, opened, clicked, bounced, dropped, spam_reported, unsubscribed

    [Required]
    public Guid SentBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ClickedAt { get; set; }

    public DateTime? BouncedAt { get; set; }

    public DateTime? DroppedAt { get; set; }

    public DateTime? SpamReportedAt { get; set; }

    public DateTime? UnsubscribedAt { get; set; }

    [MaxLength(255)]
    public string? BounceReason { get; set; }

    [MaxLength(255)]
    public string? DropReason { get; set; }

    [MaxLength(100)]
    public string? TrackingId { get; set; }

    [MaxLength(100)]
    public string? SendGridMessageId { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [MaxLength(255)]
    public string? RecipientEmail { get; set; }

    [MaxLength(255)]
    public string? RecipientName { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; } = 0;

    public DateTime? LastRetryAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("LetterTypeDefinitionId")]
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;

    [ForeignKey("ExcelUploadId")]
    public virtual ExcelUpload ExcelUpload { get; set; } = null!;

    [ForeignKey("DocumentId")]
    public virtual GeneratedDocument? Document { get; set; }

    [ForeignKey("EmailTemplateId")]
    public virtual EmailTemplate? EmailTemplate { get; set; }

    [ForeignKey("SentBy")]
    public virtual User SentByUser { get; set; } = null!;
}
