using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Core.Entities;

[Table("FileReferences")]
public class FileReference
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // "template", "signature", "excel", "document"

    [MaxLength(50)]
    public string? SubCategory { get; set; }

    [Required]
    public Guid UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsTemporary { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public int Version { get; set; } = 1;

    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? Placeholders { get; set; } // JSON

    public Guid? ParentId { get; set; }

    [MaxLength(50)]
    public string? ParentType { get; set; }

    // Navigation properties
    [ForeignKey("UploadedBy")]
    public virtual User UploadedByUser { get; set; } = null!;

    public virtual ICollection<DocumentTemplate> DocumentTemplates { get; set; } = new List<DocumentTemplate>();
    public virtual ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    public virtual ICollection<ExcelUpload> ExcelUploads { get; set; } = new List<ExcelUpload>();
    public virtual ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
}
