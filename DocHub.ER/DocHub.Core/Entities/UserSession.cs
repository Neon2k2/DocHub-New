using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("UserSessions")]
public class UserSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DeviceName { get; set; }

    [MaxLength(50)]
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet

    [MaxLength(100)]
    public string? BrowserName { get; set; }

    [MaxLength(50)]
    public string? OperatingSystem { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? LoggedOutAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt || LoggedOutAt.HasValue;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
