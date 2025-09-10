using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class EmailTemplate
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public string? HtmlBody { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = "Generic";

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "General";

    public string? Placeholders { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
}
