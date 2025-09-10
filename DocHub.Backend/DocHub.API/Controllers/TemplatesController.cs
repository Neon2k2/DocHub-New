using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all document templates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DocumentTemplateSummary>>>> GetTemplates(
        [FromQuery] string? type = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var templates = await _templateService.GetTemplatesAsync(type, isActive);

            return Ok(new ApiResponse<List<DocumentTemplateSummary>>
            {
                Success = true,
                Data = templates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document templates");
            return StatusCode(500, new ApiResponse<List<DocumentTemplateSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get document templates"
                }
            });
        }
    }

    /// <summary>
    /// Get document template by ID
    /// </summary>
    [HttpGet("{templateId}")]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDetail>>> GetTemplate(Guid templateId)
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(templateId);

            return Ok(new ApiResponse<DocumentTemplateDetail>
            {
                Success = true,
                Data = template
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<DocumentTemplateDetail>
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
            _logger.LogError(ex, "Failed to get document template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<DocumentTemplateDetail>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get document template"
                }
            });
        }
    }

    /// <summary>
    /// Create document template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DocumentTemplateSummary>>> CreateTemplate([FromBody] CreateDocumentTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DocumentTemplateSummary>
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

            var template = await _templateService.CreateTemplateAsync(request);

            return Ok(new ApiResponse<DocumentTemplateSummary>
            {
                Success = true,
                Data = template
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document template");
            return StatusCode(500, new ApiResponse<DocumentTemplateSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create document template"
                }
            });
        }
    }

    /// <summary>
    /// Update document template
    /// </summary>
    [HttpPut("{templateId}")]
    public async Task<ActionResult<ApiResponse<DocumentTemplateSummary>>> UpdateTemplate(
        Guid templateId,
        [FromBody] UpdateDocumentTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DocumentTemplateSummary>
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

            var template = await _templateService.UpdateTemplateAsync(templateId, request);

            return Ok(new ApiResponse<DocumentTemplateSummary>
            {
                Success = true,
                Data = template
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<DocumentTemplateSummary>
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
            _logger.LogError(ex, "Failed to update document template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<DocumentTemplateSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update document template"
                }
            });
        }
    }

    /// <summary>
    /// Delete document template
    /// </summary>
    [HttpDelete("{templateId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTemplate(Guid templateId)
    {
        try
        {
            await _templateService.DeleteTemplateAsync(templateId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Template deleted successfully" }
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
            _logger.LogError(ex, "Failed to delete document template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete document template"
                }
            });
        }
    }

    /// <summary>
    /// Upload document template file
    /// </summary>
    [HttpPost("{templateId}/upload")]
    public async Task<ActionResult<ApiResponse<DocumentTemplateSummary>>> UploadTemplate(
        Guid templateId,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<DocumentTemplateSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "No file provided"
                    }
                });
            }

            if (!IsValidDocumentFile(file))
            {
                return BadRequest(new ApiResponse<DocumentTemplateSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid file format. Only DOCX files are allowed"
                    }
                });
            }

            var template = await _templateService.UploadTemplateAsync(templateId, file);

            return Ok(new ApiResponse<DocumentTemplateSummary>
            {
                Success = true,
                Data = template
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<DocumentTemplateSummary>
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
            _logger.LogError(ex, "Failed to upload document template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<DocumentTemplateSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to upload document template"
                }
            });
        }
    }

    /// <summary>
    /// Download document template file
    /// </summary>
    [HttpGet("{templateId}/download")]
    public async Task<IActionResult> DownloadTemplate(Guid templateId)
    {
        try
        {
            var fileBytes = await _templateService.DownloadTemplateAsync(templateId);
            var fileName = $"template_{templateId}.docx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
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
            _logger.LogError(ex, "Failed to download document template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to download document template"
                }
            });
        }
    }

    /// <summary>
    /// Get template placeholders
    /// </summary>
    [HttpGet("{templateId}/placeholders")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetPlaceholders(Guid templateId)
    {
        try
        {
            var placeholders = await _templateService.GetPlaceholdersAsync(templateId);

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = placeholders
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<List<string>>
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
            _logger.LogError(ex, "Failed to get template placeholders for {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get template placeholders"
                }
            });
        }
    }

    /// <summary>
    /// Validate template against letter type fields
    /// </summary>
    [HttpPost("{templateId}/validate")]
    public async Task<ActionResult<ApiResponse<TemplateValidationResult>>> ValidateTemplate(
        Guid templateId,
        [FromBody] TemplateValidationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<TemplateValidationResult>
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

            var result = await _templateService.ValidateTemplateAsync(templateId, request);

            return Ok(new ApiResponse<TemplateValidationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<TemplateValidationResult>
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
            _logger.LogError(ex, "Failed to validate template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<TemplateValidationResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to validate template"
                }
            });
        }
    }

    private static bool IsValidDocumentFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".docx" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }
}
