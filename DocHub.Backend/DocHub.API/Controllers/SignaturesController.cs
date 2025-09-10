using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SignaturesController : ControllerBase
{
    private readonly ISignatureService _signatureService;
    private readonly ILogger<SignaturesController> _logger;

    public SignaturesController(
        ISignatureService signatureService,
        ILogger<SignaturesController> logger)
    {
        _signatureService = signatureService;
        _logger = logger;
    }

    /// <summary>
    /// Get all signatures
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SignatureSummary>>>> GetSignatures()
    {
        try
        {
            var signatures = await _signatureService.GetSignaturesAsync();

            return Ok(new ApiResponse<List<SignatureSummary>>
            {
                Success = true,
                Data = signatures
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get signatures");
            return StatusCode(500, new ApiResponse<List<SignatureSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get signatures"
                }
            });
        }
    }

    /// <summary>
    /// Get signature by ID
    /// </summary>
    [HttpGet("{signatureId}")]
    public async Task<ActionResult<ApiResponse<SignatureDetail>>> GetSignature(Guid signatureId)
    {
        try
        {
            var signature = await _signatureService.GetSignatureAsync(signatureId);

            return Ok(new ApiResponse<SignatureDetail>
            {
                Success = true,
                Data = signature
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<SignatureDetail>
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
            _logger.LogError(ex, "Failed to get signature {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<SignatureDetail>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get signature"
                }
            });
        }
    }

    /// <summary>
    /// Create signature
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SignatureSummary>>> CreateSignature([FromBody] CreateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<SignatureSummary>
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

            var signature = await _signatureService.CreateSignatureAsync(request);

            return Ok(new ApiResponse<SignatureSummary>
            {
                Success = true,
                Data = signature
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create signature");
            return StatusCode(500, new ApiResponse<SignatureSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create signature"
                }
            });
        }
    }

    /// <summary>
    /// Update signature
    /// </summary>
    [HttpPut("{signatureId}")]
    public async Task<ActionResult<ApiResponse<SignatureSummary>>> UpdateSignature(
        Guid signatureId,
        [FromBody] UpdateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<SignatureSummary>
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

            var signature = await _signatureService.UpdateSignatureAsync(signatureId, request);

            return Ok(new ApiResponse<SignatureSummary>
            {
                Success = true,
                Data = signature
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<SignatureSummary>
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
            _logger.LogError(ex, "Failed to update signature {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<SignatureSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update signature"
                }
            });
        }
    }

    /// <summary>
    /// Delete signature
    /// </summary>
    [HttpDelete("{signatureId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSignature(Guid signatureId)
    {
        try
        {
            await _signatureService.DeleteSignatureAsync(signatureId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Signature deleted successfully" }
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
            _logger.LogError(ex, "Failed to delete signature {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete signature"
                }
            });
        }
    }

    /// <summary>
    /// Upload signature file
    /// </summary>
    [HttpPost("{signatureId}/upload")]
    public async Task<ActionResult<ApiResponse<SignatureSummary>>> UploadSignature(
        Guid signatureId,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<SignatureSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "No file provided"
                    }
                });
            }

            if (!IsValidImageFile(file))
            {
                return BadRequest(new ApiResponse<SignatureSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid file format. Only JPEG and PNG files are allowed"
                    }
                });
            }

            var signature = await _signatureService.UploadSignatureAsync(signatureId, file);

            return Ok(new ApiResponse<SignatureSummary>
            {
                Success = true,
                Data = signature
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<SignatureSummary>
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
            _logger.LogError(ex, "Failed to upload signature {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<SignatureSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to upload signature"
                }
            });
        }
    }

    /// <summary>
    /// Download signature file
    /// </summary>
    [HttpGet("{signatureId}/download")]
    public async Task<IActionResult> DownloadSignature(Guid signatureId)
    {
        try
        {
            var fileBytes = await _signatureService.DownloadSignatureAsync(signatureId);
            var fileName = $"signature_{signatureId}.png";

            return File(fileBytes, "image/png", fileName);
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
            _logger.LogError(ex, "Failed to download signature {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to download signature"
                }
            });
        }
    }

    /// <summary>
    /// Process signature (remove watermark, optimize)
    /// </summary>
    [HttpPost("{signatureId}/process")]
    public async Task<ActionResult<ApiResponse<SignatureSummary>>> ProcessSignature(Guid signatureId)
    {
        try
        {
            var signature = await _signatureService.ProcessSignatureAsync(signatureId);

            return Ok(new ApiResponse<SignatureSummary>
            {
                Success = true,
                Data = signature
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<SignatureSummary>
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
            _logger.LogError(ex, "Failed to process signature {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<SignatureSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to process signature"
                }
            });
        }
    }

    /// <summary>
    /// Get signature preview URL
    /// </summary>
    [HttpGet("{signatureId}/preview")]
    public async Task<ActionResult<ApiResponse<string>>> GetPreviewUrl(Guid signatureId)
    {
        try
        {
            var previewUrl = await _signatureService.GetPreviewUrlAsync(signatureId);

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = previewUrl
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
            _logger.LogError(ex, "Failed to get signature preview URL for {SignatureId}", signatureId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get preview URL"
                }
            });
        }
    }

    private static bool IsValidImageFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }
}
