using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class ExcelUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid UploadId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int RowsProcessed { get; set; }
}

public class ExcelParseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public int RowsProcessed { get; set; }
}

public class ExcelUploadSummary
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}

public class ExcelDataResult
{
    public bool Success { get; set; }
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public int TotalRows { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ExcelValidationRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public List<Dictionary<string, object>> Data { get; set; } = new();
}

public class ExcelValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ValidationError> RowErrors { get; set; } = new();
}

public class ValidationError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
