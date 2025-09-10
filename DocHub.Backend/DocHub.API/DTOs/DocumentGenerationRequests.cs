using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class DocumentGenerationRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public List<Guid> EmployeeIds { get; set; } = new();

    public Guid? TemplateId { get; set; }

    public Guid? SignatureId { get; set; }

    public bool IncludeDocumentAttachments { get; set; } = true;

    public Dictionary<string, object>? AdditionalFieldData { get; set; }

    [Required]
    public string GeneratedBy { get; set; } = string.Empty;
}

public class SingleDocumentGenerationRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    public Guid? TemplateId { get; set; }

    public Guid? SignatureId { get; set; }

    public bool IncludeDocumentAttachments { get; set; } = true;

    public Dictionary<string, object>? AdditionalFieldData { get; set; }

    [Required]
    public string GeneratedBy { get; set; } = string.Empty;
}

public class DocumentPreviewRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    public Guid? TemplateId { get; set; }

    public Guid? SignatureId { get; set; }

    public Dictionary<string, object>? AdditionalFieldData { get; set; }

    [Required]
    public string GeneratedBy { get; set; } = string.Empty;
}

public class DocumentValidationRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public List<Guid> EmployeeIds { get; set; } = new();

    public Guid? TemplateId { get; set; }

    public Guid? SignatureId { get; set; }

    public Dictionary<string, object>? AdditionalFieldData { get; set; }
}

public class DocumentGenerationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<GeneratedDocumentSummary> GeneratedDocuments { get; set; } = new();
    public int TotalDocuments { get; set; }
    public int SuccessfulDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class DocumentPreviewResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> PlaceholderData { get; set; } = new();
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidationData { get; set; } = new();
}

public class GeneratedDocumentSummary
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
