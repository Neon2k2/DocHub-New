using System.ComponentModel.DataAnnotations;

namespace DocHub.API.Models;

/// <summary>
/// Represents employee data specific to a tab/letter type
/// This allows each tab to have its own employee data with different columns
/// </summary>
public class TabEmployeeData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid TabId { get; set; } // FK to LetterTypeDefinition
    
    [Required]
    [MaxLength(100)]
    public string EmployeeId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Email { get; set; }
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    /// <summary>
    /// JSON field to store additional custom fields specific to this tab
    /// Example: {"experience_years": 5, "previous_company": "ABC Corp", "skills": ["C#", "React"]}
    /// </summary>
    public string? CustomFields { get; set; }
    
    /// <summary>
    /// Whether this employee is active for this specific tab
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Source of the data (excel, database, manual)
    /// </summary>
    [MaxLength(50)]
    public string DataSource { get; set; } = "manual";
    
    /// <summary>
    /// Reference to uploaded Excel file if data came from Excel
    /// </summary>
    public Guid? ExcelFileId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public LetterTypeDefinition? Tab { get; set; }
}
