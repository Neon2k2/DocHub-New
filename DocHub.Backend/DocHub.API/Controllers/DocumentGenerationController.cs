using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DocumentGenerationController : ControllerBase
{
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<DocumentGenerationController> _logger;

    public DocumentGenerationController(
        IDocumentGenerationService documentGenerationService,
        ILogger<DocumentGenerationController> logger)
    {
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// Generate documents for multiple employees
    /// </summary>
    [HttpPost("generate-bulk")]
    public async Task<ActionResult<ApiResponse<DocumentGenerationResult>>> GenerateBulk([FromBody] DocumentGenerationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DocumentGenerationResult>
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

            var result = await _documentGenerationService.GenerateBulkAsync(request);

            return Ok(new ApiResponse<DocumentGenerationResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate bulk documents");
            return StatusCode(500, new ApiResponse<DocumentGenerationResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to generate documents"
                }
            });
        }
    }

    /// <summary>
    /// Generate document for a single employee
    /// </summary>
    [HttpPost("generate-single")]
    public async Task<ActionResult<ApiResponse<DocumentGenerationResult>>> GenerateSingle([FromBody] SingleDocumentGenerationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DocumentGenerationResult>
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

            var result = await _documentGenerationService.GenerateSingleAsync(request);

            return Ok(new ApiResponse<DocumentGenerationResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate single document");
            return StatusCode(500, new ApiResponse<DocumentGenerationResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to generate document"
                }
            });
        }
    }

    /// <summary>
    /// Preview document for an employee
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<DocumentPreviewResult>>> Preview([FromBody] DocumentPreviewRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DocumentPreviewResult>
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

            var result = await _documentGenerationService.PreviewAsync(request);

            return Ok(new ApiResponse<DocumentPreviewResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview document");
            return StatusCode(500, new ApiResponse<DocumentPreviewResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to preview document"
                }
            });
        }
    }

    /// <summary>
    /// Validate document generation request
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<ValidationResult>>> Validate([FromBody] DocumentValidationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ValidationResult>
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

            var result = await _documentGenerationService.ValidateAsync(request);

            return Ok(new ApiResponse<ValidationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate document generation request");
            return StatusCode(500, new ApiResponse<ValidationResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to validate request"
                }
            });
        }
    }

    /// <summary>
    /// Get download URL for a generated document
    /// </summary>
    [HttpGet("{documentId}/download-url")]
    public async Task<ActionResult<ApiResponse<string>>> GetDownloadUrl(Guid documentId, [FromQuery] string format = "pdf")
    {
        try
        {
            var downloadUrl = await _documentGenerationService.GetDownloadUrlAsync(documentId, format);

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = downloadUrl
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<string>
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
            _logger.LogError(ex, "Failed to get download URL for document {DocumentId}", documentId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get download URL"
                }
            });
        }
    }

    /// <summary>
    /// Download a generated document
    /// </summary>
    [HttpGet("{documentId}/download")]
    public async Task<IActionResult> Download(Guid documentId, [FromQuery] string format = "pdf")
    {
        try
        {
            var fileBytes = await _documentGenerationService.DownloadDocumentAsync(documentId, format);
            var fileName = $"document_{documentId}.{format}";

            return File(fileBytes, "application/octet-stream", fileName);
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
        catch (FileNotFoundException ex)
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
            _logger.LogError(ex, "Failed to download document {DocumentId}", documentId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to download document"
                }
            });
        }
    }
}
