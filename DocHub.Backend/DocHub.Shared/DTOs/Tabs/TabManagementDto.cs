using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Tabs;

public class CreateLetterTypeRequest
{
    [Required]
    [MaxLength(50)]
    public string TypeKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string DataSourceType { get; set; } = "Database"; // "Database" or "Excel"

    public string? FieldConfiguration { get; set; } // JSON

    public string? TableSchema { get; set; } // JSON

    public List<CreateDynamicFieldRequest> Fields { get; set; } = new List<CreateDynamicFieldRequest>();
}

public class UpdateLetterTypeRequest
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? DataSourceType { get; set; }

    public string? FieldConfiguration { get; set; } // JSON

    public string? TableSchema { get; set; } // JSON

    public bool? IsActive { get; set; }
}

public class CreateDynamicFieldRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string FieldType { get; set; } = string.Empty; // "Text", "Number", "Date", etc.

    public bool IsRequired { get; set; } = false;

    public string? ValidationRules { get; set; } // JSON

    public string? DefaultValue { get; set; }

    public int OrderIndex { get; set; } = 0;
}

public class UpdateDynamicFieldRequest
{
    [MaxLength(100)]
    public string? FieldName { get; set; }

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(20)]
    public string? FieldType { get; set; }

    public bool? IsRequired { get; set; }

    public string? ValidationRules { get; set; } // JSON

    public string? DefaultValue { get; set; }

    public int? OrderIndex { get; set; }
}


public class LetterTypeDefinitionDto
{
    public Guid Id { get; set; }
    public string TypeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? FieldConfiguration { get; set; }
    public string? TableSchema { get; set; }
    public bool IsActive { get; set; }
    public List<DynamicFieldDto> Fields { get; set; } = new List<DynamicFieldDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DynamicFieldDto
{
    public Guid Id { get; set; }
    public Guid LetterTypeDefinitionId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


// Frontend-compatible DTOs
public class DynamicTabDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public List<DynamicTabFieldDto> Fields { get; set; } = new List<DynamicTabFieldDto>();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("dataSource")]
    public string DataSource { get; set; } = "database";

    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }

    [JsonPropertyName("recordCount")]
    public int RecordCount { get; set; }
}

public class DynamicTabFieldDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("placeholder")]
    public string Placeholder { get; set; } = string.Empty;

    [JsonPropertyName("validation")]
    public object? Validation { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

public class CreateDynamicTabRequest
{
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public List<CreateDynamicTabFieldRequest> Fields { get; set; } = new List<CreateDynamicTabFieldRequest>();

    [JsonPropertyName("dataSource")]
    public string DataSource { get; set; } = "database";
}

public class CreateDynamicTabFieldRequest
{
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [Required]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("placeholder")]
    public string Placeholder { get; set; } = string.Empty;

    [JsonPropertyName("validation")]
    public object? Validation { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

public class UpdateDynamicTabRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("fields")]
    public List<CreateDynamicTabFieldRequest>? Fields { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}