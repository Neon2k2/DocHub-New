using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;
    
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
