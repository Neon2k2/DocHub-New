using System.ComponentModel.DataAnnotations;

namespace DocHub.API.DTOs;

public class CreateDynamicFieldRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text";

    public bool IsRequired { get; set; } = false;

    public string? ValidationRules { get; set; }

    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    public int Order { get; set; } = 0;
}

public class UpdateDynamicFieldRequest
{
    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text";

    public bool IsRequired { get; set; } = false;

    public string? ValidationRules { get; set; }

    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    public int Order { get; set; } = 0;
}

public class ReorderFieldsRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public List<FieldOrder> FieldOrders { get; set; } = new();
}

public class FieldOrder
{
    [Required]
    public Guid FieldId { get; set; }

    [Required]
    public int Order { get; set; }
}

public class ValidateFieldRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text";

    public bool IsRequired { get; set; } = false;

    public string? ValidationRules { get; set; }

    [MaxLength(500)]
    public string? DefaultValue { get; set; }
}

public class FieldValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Suggestions { get; set; } = new();
}
