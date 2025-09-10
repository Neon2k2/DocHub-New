using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class WebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string EmailJobId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Status { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(1000)]
    public string? Response { get; set; }
    
    public int Attempt { get; set; }
    
    [MaxLength(100)]
    public string? SgEventId { get; set; }
    
    [MaxLength(100)]
    public string? SgMessageId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    [MaxLength(1000)]
    public string? UniqueArgs { get; set; }
    
    [MaxLength(500)]
    public string? Url { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(45)]
    public string? Ip { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
