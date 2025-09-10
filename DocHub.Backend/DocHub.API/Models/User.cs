using System.ComponentModel.DataAnnotations;

namespace DocHub.API.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";
    
    public bool IsEmailVerified { get; set; } = false;
    
    public DateTime? LastLoginAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
