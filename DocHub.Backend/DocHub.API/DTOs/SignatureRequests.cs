using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class SignatureSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SignatureDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSignatureRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateSignatureRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }
}
