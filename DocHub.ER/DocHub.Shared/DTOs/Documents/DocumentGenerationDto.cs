using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace DocHub.Shared.DTOs.Documents;

public class GenerateDocumentRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public Guid? ExcelUploadId { get; set; }

    [Required]
    public Guid TemplateId { get; set; }

    public Guid? SignatureId { get; set; }
    public string? ProcessingOptions { get; set; } // JSON
}

// Frontend-compatible request structure
public class FrontendGenerateDocumentRequest
{
    [JsonPropertyName("templateId")]
    public Guid TemplateId { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("signatureId")]
    public Guid? SignatureId { get; set; }

    [JsonPropertyName("placeholderData")]
    public Dictionary<string, object> PlaceholderData { get; set; } = new Dictionary<string, object>();
}

public class GenerateBulkDocumentsRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public List<Guid> ExcelUploadIds { get; set; } = new List<Guid>();

    [Required]
    public Guid TemplateId { get; set; }

    public Guid? SignatureId { get; set; }
    public string? ProcessingOptions { get; set; } // JSON
}

public class PreviewDocumentRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public Guid? ExcelUploadId { get; set; }

    [Required]
    public Guid TemplateId { get; set; }

    public Guid? SignatureId { get; set; }
    public string? ProcessingOptions { get; set; } // JSON
}
public class DocumentTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Placeholders { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
public class ProcessTemplateRequest
{
    [Required]
    public Guid TemplateId { get; set; }

    public string? ProcessingOptions { get; set; } // JSON
}

public class InsertSignatureRequest
{
    [Required]
    public Guid DocumentId { get; set; }

    [Required]
    public Guid SignatureId { get; set; }

    public string? Position { get; set; } // JSON coordinates
    public string? ProcessingOptions { get; set; } // JSON
}

public class GeneratedDocumentDto
{
    public Guid Id { get; set; }
    public Guid LetterTypeDefinitionId { get; set; }
    public string LetterTypeName { get; set; } = string.Empty;
    public Guid? ExcelUploadId { get; set; }
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public Guid? SignatureId { get; set; }
    public string? SignatureName { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public Guid GeneratedBy { get; set; }
    public string GeneratedByName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string? Metadata { get; set; }
}

public class DocumentPreviewDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string PreviewUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string? Metadata { get; set; }
}

public class WatermarkRemovalOptions
{
    public string Method { get; set; } = "automatic"; // "automatic", "manual"
    public string? ManualCoordinates { get; set; } // JSON
    public int Threshold { get; set; } = 50;
    public bool PreserveQuality { get; set; } = true;
    public string? OutputFormat { get; set; } = "png";
}

public class ProcessSignatureRequest
{
    [Required]
    public IFormFile SignatureFile { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public WatermarkRemovalOptions WatermarkRemoval { get; set; } = new WatermarkRemovalOptions();
}

public class ValidateSignatureRequest
{
    [Required]
    public byte[] SignatureData { get; set; } = Array.Empty<byte>();
}

public class ProcessDocumentRequest
{
    [Required]
    public Guid DocumentId { get; set; }

    public string? ProcessingOptions { get; set; }
}