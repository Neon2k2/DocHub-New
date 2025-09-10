using System.ComponentModel.DataAnnotations;

namespace DocHub.API.Models;

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid RoleId { get; set; }
    
    [MaxLength(100)]
    public string? ModuleAccess { get; set; } // ER, Billing, Timesheet, Admin
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
