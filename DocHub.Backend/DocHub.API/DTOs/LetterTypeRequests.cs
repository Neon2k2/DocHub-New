using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class CreateLetterTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string TypeKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? FieldConfiguration { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = "ER";
}

public class UpdateLetterTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string TypeKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? FieldConfiguration { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = "ER";
}
