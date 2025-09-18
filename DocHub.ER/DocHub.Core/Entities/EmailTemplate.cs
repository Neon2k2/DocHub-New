using System.ComponentModel.DataAnnotations;

namespace DocHub.Core.Entities;

public class EmailTemplate
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    [Required]
    public Guid CreatedBy { get; set; }
    
    // Navigation property
    public LetterTypeDefinition? LetterTypeDefinition { get; set; }
}
