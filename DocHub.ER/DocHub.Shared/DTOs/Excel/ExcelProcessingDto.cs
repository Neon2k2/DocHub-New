using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace DocHub.Shared.DTOs.Excel;

public class ProcessExcelRequest
{
    [Required]
    public Guid UploadId { get; set; }

    public string? ProcessingOptions { get; set; } // JSON
}

public class FieldMappingRequest
{
    [Required]
    public Guid UploadId { get; set; }

    public string? SuggestedMappings { get; set; } // JSON
}

public class ValidateExcelRequest
{
    [Required]
    public Guid UploadId { get; set; }

    public string? ValidationRules { get; set; } // JSON
}

public class GenerateTemplateRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    public string? TemplateOptions { get; set; } // JSON
}

public class ExcelUploadDto
{
    public Guid Id { get; set; }
    public Guid LetterTypeDefinitionId { get; set; }
    public string LetterTypeName { get; set; } = string.Empty;
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Metadata { get; set; }
    public string? ParsedData { get; set; }
    public string? FieldMappings { get; set; }
    public string? ProcessingOptions { get; set; }
    public string? Results { get; set; }
    public bool IsProcessed { get; set; }
    public int ProcessedRows { get; set; }
    public Guid ProcessedBy { get; set; }
    public string ProcessedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ExcelDataDto
{
    public Guid UploadId { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
    public List<string> Headers { get; set; } = new List<string>();
    public int TotalRows { get; set; }
    public string? ValidationErrors { get; set; }
    public bool IsValid { get; set; }
}

public class FieldMappingDto
{
    public string ExcelColumn { get; set; } = string.Empty;
    public string DynamicField { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? ValidationRules { get; set; }
    public double Confidence { get; set; } // 0-1 confidence score
}

public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
    public List<ValidationWarningDto> Warnings { get; set; } = new List<ValidationWarningDto>();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
}

public class ValidationErrorDto
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty; // "required", "format", "range", etc.
}

public class ValidationWarningDto
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty; // "duplicate", "suggestion", etc.
}

public class ExcelProcessingResultDto
{
    public Guid UploadId { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int ProcessedRows { get; set; }
    public int TotalRows { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
    public string? Results { get; set; } // JSON
}

public class ExcelValidationResultDto
{
    public bool IsValid { get; set; }
    public ValidationResultDto ValidationResult { get; set; } = new ValidationResultDto();
    public List<string> Headers { get; set; } = new List<string>();
    public int TotalRows { get; set; }
}

public class FieldMappingResultDto
{
    public List<FieldMappingDto> Mappings { get; set; } = new List<FieldMappingDto>();
    public double OverallConfidence { get; set; }
    public List<string> UnmappedFields { get; set; } = new List<string>();
    public List<string> SuggestedMappings { get; set; } = new List<string>();
}

public class ImportResultDto
{
    public bool IsSuccess { get; set; }
    public int ImportedRows { get; set; }
    public int FailedRows { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
    public string? Message { get; set; }
}

public class ExcelTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid LetterTypeDefinitionId { get; set; }
    public string LetterTypeName { get; set; } = string.Empty;
    public string TemplateData { get; set; } = string.Empty; // JSON
    public string FieldMappings { get; set; } = string.Empty; // JSON
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateExcelTemplateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    public string? TemplateData { get; set; } // JSON
    public string? FieldMappings { get; set; } // JSON
}

public class UpdateExcelTemplateRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public string? TemplateData { get; set; } // JSON
    public string? FieldMappings { get; set; } // JSON
    public bool? IsActive { get; set; }
}

public class MapFieldsRequest
{
    [Required]
    public Guid UploadId { get; set; }

    public List<FieldMappingDto> Mappings { get; set; } = new List<FieldMappingDto>();
    public bool AutoMap { get; set; } = false;
}

public class ImportDataRequest
{
    [Required]
    public Guid UploadId { get; set; }

    public List<FieldMappingDto> Mappings { get; set; } = new List<FieldMappingDto>();
    public bool ValidateOnly { get; set; } = false;
    public string? ProcessingOptions { get; set; } // JSON
}

public class ExcelPreviewDto
{
    public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
    public List<string> Headers { get; set; } = new List<string>();
    public int TotalRows { get; set; }
    public int PreviewRows { get; set; }
}

public class ExcelAnalyticsDto
{
    public int TotalUploads { get; set; }
    public int ProcessedUploads { get; set; }
    public int FailedUploads { get; set; }
    public int TotalRowsProcessed { get; set; }
    public double AverageProcessingTime { get; set; }
    public Dictionary<string, int> UploadsByMonth { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> ProcessingStatusCounts { get; set; } = new Dictionary<string, int>();
}

// Frontend-compatible Excel DTOs
public class FrontendExcelUploadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public FrontendExcelData? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class FrontendExcelData
{
    [JsonPropertyName("headers")]
    public List<string> Headers { get; set; } = new List<string>();

    [JsonPropertyName("data")]
    public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }

    [JsonPropertyName("id")]
    public Guid Id { get; set; }
}
