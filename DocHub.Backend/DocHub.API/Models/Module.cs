using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.Models;

public class Module
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<LetterTypeDefinition> LetterTypeDefinitions { get; set; } = new List<LetterTypeDefinition>();
    public virtual ICollection<DocumentTemplate> DocumentTemplates { get; set; } = new List<DocumentTemplate>();
    public virtual ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    public virtual ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
    public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
}
