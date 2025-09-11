using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string EmployeeId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Email { get; set; }
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Position { get; set; } = string.Empty;
    
    public DateTime JoiningDate { get; set; }
    
    public DateTime? RelievingDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(200)]
    public string? Manager { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    public decimal? Salary { get; set; }
    
    // Computed property for full name
    public string Name => $"{FirstName} {LastName}";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
