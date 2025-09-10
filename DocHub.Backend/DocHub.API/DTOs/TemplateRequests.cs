using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class DocumentTemplateSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentTemplateDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public List<string> Placeholders { get; set; } = new();
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDocumentTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    public List<string>? Placeholders { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateDocumentTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    public List<string>? Placeholders { get; set; }

    public bool IsActive { get; set; } = true;

    public int? Version { get; set; }
}

public class TemplateValidationRequest
{
    public List<string>? RequiredFields { get; set; }
}

public class TemplateValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Placeholders { get; set; } = new();
    public List<string> MissingFields { get; set; } = new();
    public List<string> ExtraFields { get; set; } = new();
}
