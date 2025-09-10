using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;

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
}
