using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("GeneratedDocuments")]
public class GeneratedDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    public Guid? ExcelUploadId { get; set; }

    [Required]
    public Guid TemplateId { get; set; }

    public Guid? SignatureId { get; set; }

    [Required]
    public Guid FileId { get; set; }

    [Required]
    public Guid GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; } // JSON

    // Navigation properties
    [ForeignKey("LetterTypeDefinitionId")]
    public virtual LetterTypeDefinition LetterTypeDefinition { get; set; } = null!;

    [ForeignKey("ExcelUploadId")]
    public virtual ExcelUpload? ExcelUpload { get; set; }

    [ForeignKey("TemplateId")]
    public virtual DocumentTemplate Template { get; set; } = null!;

    [ForeignKey("SignatureId")]
    public virtual Signature? Signature { get; set; }

    [ForeignKey("FileId")]
    public virtual FileReference File { get; set; } = null!;

    [ForeignKey("GeneratedBy")]
    public virtual User GeneratedByUser { get; set; } = null!;

    public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
}
