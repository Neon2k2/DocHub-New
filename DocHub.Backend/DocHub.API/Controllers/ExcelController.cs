using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;
using System.Text.Json;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly IExcelService _excelService;
    private readonly ILogger<ExcelController> _logger;

    public ExcelController(
        IExcelService excelService,
        ILogger<ExcelController> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    /// <summary>
    /// Upload Excel file for a letter type
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<ExcelUploadResult>>> Upload(
        [FromForm] IFormFile file,
        [FromForm] Guid letterTypeDefinitionId,
        [FromForm] string? description = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<ExcelUploadResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "No file provided"
                    }
                });
            }

            if (!IsValidExcelFile(file))
            {
                return BadRequest(new ApiResponse<ExcelUploadResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid file format. Only Excel files (.xlsx, .xls) are allowed"
                    }
                });
            }

            var result = await _excelService.UploadAsync(file, letterTypeDefinitionId, description);

            return Ok(new ApiResponse<ExcelUploadResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<ExcelUploadResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "VALIDATION_ERROR",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload Excel file");
            return StatusCode(500, new ApiResponse<ExcelUploadResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to upload Excel file"
                }
            });
        }
    }

    /// <summary>
    /// Parse Excel file and return preview data
    /// </summary>
    [HttpPost("parse")]
    public async Task<ActionResult<ApiResponse<ExcelParseResult>>> Parse([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<ExcelParseResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "No file provided"
                    }
                });
            }

            if (!IsValidExcelFile(file))
            {
                return BadRequest(new ApiResponse<ExcelParseResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid file format. Only Excel files (.xlsx, .xls) are allowed"
                    }
                });
            }

            var result = await _excelService.ParseAsync(file);

            return Ok(new ApiResponse<ExcelParseResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Excel file");
            return StatusCode(500, new ApiResponse<ExcelParseResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to parse Excel file"
                }
            });
        }
    }

    /// <summary>
    /// Get Excel uploads for a letter type
    /// </summary>
    [HttpGet("uploads/{letterTypeDefinitionId}")]
    public async Task<ActionResult<ApiResponse<List<ExcelUploadSummary>>>> GetUploads(Guid letterTypeDefinitionId)
    {
        try
        {
            var uploads = await _excelService.GetUploadsAsync(letterTypeDefinitionId);

            return Ok(new ApiResponse<List<ExcelUploadSummary>>
            {
                Success = true,
                Data = uploads
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Excel uploads for letter type {LetterTypeDefinitionId}", letterTypeDefinitionId);
            return StatusCode(500, new ApiResponse<List<ExcelUploadSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get Excel uploads"
                }
            });
        }
    }

    /// <summary>
    /// Get Excel data for a specific upload
    /// </summary>
    [HttpGet("uploads/{uploadId}/data")]
    public async Task<ActionResult<ApiResponse<ExcelDataResult>>> GetUploadData(
        Guid uploadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var data = await _excelService.GetUploadDataAsync(uploadId, page, pageSize);

            return Ok(new ApiResponse<ExcelDataResult>
            {
                Success = true,
                Data = data
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ExcelDataResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Excel data for upload {UploadId}", uploadId);
            return StatusCode(500, new ApiResponse<ExcelDataResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get Excel data"
                }
            });
        }
    }

    /// <summary>
    /// Delete Excel upload
    /// </summary>
    [HttpDelete("uploads/{uploadId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUpload(Guid uploadId)
    {
        try
        {
            await _excelService.DeleteUploadAsync(uploadId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Upload deleted successfully" }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Excel upload {UploadId}", uploadId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete upload"
                }
            });
        }
    }

    /// <summary>
    /// Get Excel data for a specific tab
    /// </summary>
    [HttpGet("tab/{tabId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetExcelDataForTab(Guid tabId)
    {
        try
        {
            // Get tab employee service
            var tabEmployeeService = HttpContext.RequestServices.GetRequiredService<ITabEmployeeService>();
            
            // Get employees for this tab
            var employees = await tabEmployeeService.GetEmployeesByTabIdAsync(tabId);
            
            if (!employees.Any())
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        headers = new string[0],
                        data = new object[0],
                        fileName = "",
                        fileSize = 0,
                        uploadedAt = DateTime.UtcNow,
                        rowCount = 0
                    }
                });
            }

            // Convert employees to Excel-like format
            var headers = new List<string> { "EmployeeId", "EmployeeName", "Email", "Phone", "Department", "Position" };
            var data = new List<Dictionary<string, object>>();

            foreach (var employee in employees)
            {
                var row = new Dictionary<string, object>
                {
                    ["EmployeeId"] = employee.EmployeeId ?? "",
                    ["EmployeeName"] = employee.EmployeeName ?? "",
                    ["Email"] = employee.Email ?? "",
                    ["Phone"] = employee.Phone ?? "",
                    ["Department"] = employee.Department ?? "",
                    ["Position"] = employee.Position ?? ""
                };

                // Add custom fields if they exist
                if (!string.IsNullOrEmpty(employee.CustomFields))
                {
                    try
                    {
                        var customFields = JsonSerializer.Deserialize<Dictionary<string, object>>(employee.CustomFields);
                        if (customFields != null)
                        {
                            foreach (var kvp in customFields)
                            {
                                if (!headers.Contains(kvp.Key))
                                {
                                    headers.Add(kvp.Key);
                                }
                                row[kvp.Key] = kvp.Value ?? "";
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse custom fields for employee {EmployeeId}", employee.Id);
                    }
                }

                data.Add(row);
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    headers = headers,
                    data = data,
                    fileName = "Uploaded Data",
                    fileSize = 0,
                    uploadedAt = employees.FirstOrDefault()?.CreatedAt ?? DateTime.UtcNow,
                    rowCount = data.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Excel data for tab {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get Excel data for tab"
                }
            });
        }
    }

    /// <summary>
    /// Upload Excel data for a specific tab
    /// </summary>
    [HttpPost("tab/{tabId}")]
    public async Task<ActionResult<ApiResponse<object>>> UploadExcelDataForTab(
        Guid tabId,
        [FromForm] IFormFile file,
        [FromForm] string? description = null)
    {
        try
        {
            _logger.LogInformation("UploadExcelDataForTab called with tabId: {TabId}, file: {FileName}, fileSize: {FileSize}", 
                tabId, file?.FileName, file?.Length);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for tab {TabId}", tabId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "No file provided"
                    }
                });
            }

            if (!IsValidExcelFile(file))
            {
                _logger.LogWarning("Invalid file format for tab {TabId}. File: {FileName}", tabId, file.FileName);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid file format. Only Excel files (.xlsx, .xls) are allowed"
                    }
                });
            }

            // Parse Excel file to get preview data
            _logger.LogInformation("Parsing Excel file for tab {TabId}", tabId);
            var parseResult = await _excelService.ParseAsync(file);
            _logger.LogInformation("Parse result for tab {TabId}: Success={Success}, Message={Message}", 
                tabId, parseResult.Success, parseResult.Message);
            
            if (!parseResult.Success)
            {
                _logger.LogWarning("Excel parse failed for tab {TabId}: {Message}", tabId, parseResult.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "PARSE_ERROR",
                        Message = parseResult.Message
                    }
                });
            }

            // Create intelligent column mappings based on headers
            var columnMappings = new Dictionary<string, string>();
            foreach (var header in parseResult.Headers)
            {
                // Map common Excel headers to standard field names
                var mappedField = MapHeaderToField(header);
                columnMappings[header] = mappedField;
            }

            // Import data using TabEmployeeService
            _logger.LogInformation("Starting Excel import for tab {TabId} with {HeaderCount} headers", 
                tabId, columnMappings.Count);
            var tabEmployeeService = HttpContext.RequestServices.GetRequiredService<ITabEmployeeService>();
            var importSuccess = await tabEmployeeService.ImportFromExcelAsync(tabId, file, columnMappings);
            _logger.LogInformation("Excel import result for tab {TabId}: Success={Success}", tabId, importSuccess);

            if (!importSuccess)
            {
                _logger.LogError("Excel import failed for tab {TabId}", tabId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "IMPORT_ERROR",
                        Message = "Failed to import Excel data into database"
                    }
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    message = "Excel data uploaded and processed successfully",
                    tabId = tabId,
                    fileName = file.FileName,
                    fileSize = file.Length,
                    rowsProcessed = parseResult.RowsProcessed,
                    headers = parseResult.Headers
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload Excel data for tab {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to upload Excel data for tab"
                }
            });
        }
    }

    /// <summary>
    /// Download Excel template for a letter type
    /// </summary>
    [HttpGet("templates/{letterTypeDefinitionId}")]
    public async Task<IActionResult> DownloadTemplate(Guid letterTypeDefinitionId)
    {
        try
        {
            var templateBytes = await _excelService.DownloadTemplateAsync(letterTypeDefinitionId);
            var fileName = $"template_{letterTypeDefinitionId}.xlsx";

            return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Excel template for letter type {LetterTypeDefinitionId}", letterTypeDefinitionId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to download template"
                }
            });
        }
    }

    /// <summary>
    /// Validate Excel data against letter type fields
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<ExcelValidationResult>>> Validate([FromBody] ExcelValidationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ExcelValidationResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request data",
                        Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var result = await _excelService.ValidateAsync(request);

            return Ok(new ApiResponse<ExcelValidationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Excel data");
            return StatusCode(500, new ApiResponse<ExcelValidationResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to validate Excel data"
                }
            });
        }
    }

    private static bool IsValidExcelFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }

    private static string MapHeaderToField(string header)
    {
        if (string.IsNullOrEmpty(header))
            return header;

        var headerLower = header.ToLowerInvariant().Trim();

        // Map common variations to standard field names
        switch (headerLower)
        {
            case "emp id":
            case "employee id":
            case "id":
            case "emp_id":
            case "employee_id":
                return "EmployeeId";
            
            case "emp name":
            case "employee name":
            case "name":
            case "emp_name":
            case "employee_name":
                return "EmployeeName";
            
            case "email":
            case "email address":
            case "email_address":
                return "Email";
            
            case "phone":
            case "phone number":
            case "phone_number":
            case "mobile":
            case "mobile number":
                return "Phone";
            
            case "department":
            case "dept":
                return "Department";
            
            case "position":
            case "job title":
            case "job_title":
            case "title":
            case "designation":
                return "Position";
            
            default:
                return header; // Return original header for custom fields
        }
    }
}
