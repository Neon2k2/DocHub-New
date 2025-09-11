using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class ExcelProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public Dictionary<string, string> FieldMappings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ExcelDataValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
}

public class ExcelFieldMappingResult
{
    public bool Success { get; set; }
    public Dictionary<string, string> Mappings { get; set; } = new();
    public List<string> UnmappedFields { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class ExcelTemplateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> TemplateData { get; set; } = new();
}

public class FieldMapping
{
    public Guid FieldId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? MappedColumn { get; set; }
    public double Confidence { get; set; }
}