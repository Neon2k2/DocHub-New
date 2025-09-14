using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;


namespace DocHub.Shared.DTOs.Files;

public class UploadFileRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // "template", "signature", "excel", "document"

    [MaxLength(50)]
    public string? SubCategory { get; set; }

    public Guid? ParentId { get; set; }
    public string? ParentType { get; set; }
    public bool IsTemporary { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public string? Metadata { get; set; } // JSON
}

public class UploadTemplateRequest : UploadFileRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public string? Placeholders { get; set; } // JSON
}

public class UploadSignatureRequest : UploadFileRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}

public class UploadExcelRequest : UploadFileRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    public string? ProcessingOptions { get; set; } // JSON
}

public class ProcessFileRequest
{
    public string? ProcessingOptions { get; set; } // JSON
}

public class FileReferenceDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public Guid UploadedBy { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsTemporary { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public string? Metadata { get; set; }
    public string? Placeholders { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentType { get; set; }
}



public class SignatureDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
