using DocHub.Application.Services;
using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Files;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignatureController : ControllerBase
{
    private readonly SignatureProcessingService _signatureService;
    private readonly ILogger<SignatureController> _logger;

    public SignatureController(SignatureProcessingService signatureService, ILogger<SignatureController> logger)
    {
        _signatureService = signatureService;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<ActionResult<SignatureDto>> ProcessSignature([FromForm] ProcessSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var signature = await _signatureService.ProcessSignatureAsync(request, userId);
            return Ok(signature);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature");
            return StatusCode(500, new { message = "An error occurred while processing signature" });
        }
    }

    [HttpPost("remove-watermark")]
    public async Task<ActionResult<byte[]>> RemoveWatermark([FromBody] RemoveWatermarkRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var processedSignature = await _signatureService.RemoveWatermarkAsync(request.SignatureData, request.Options);
            return Ok(new { signatureData = processedSignature });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing watermark from signature");
            return StatusCode(500, new { message = "An error occurred while removing watermark" });
        }
    }

    [HttpPost("insert-into-document")]
    public async Task<ActionResult<SignatureDto>> InsertSignatureIntoDocument([FromBody] InsertSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var signature = await _signatureService.InsertSignatureIntoDocumentAsync(request, userId);
            return Ok(signature);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting signature into document");
            return StatusCode(500, new { message = "An error occurred while inserting signature" });
        }
    }

    [HttpPost("validate-quality")]
    public async Task<ActionResult<bool>> ValidateSignatureQuality([FromBody] ValidateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _signatureService.ValidateSignatureQualityAsync(request.SignatureData);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature quality");
            return StatusCode(500, new { message = "An error occurred while validating signature quality" });
        }
    }

    [HttpPost("process-watermark-removal")]
    public async Task<ActionResult<byte[]>> ProcessWatermarkRemoval([FromBody] ProcessWatermarkRemovalRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var processedData = await _signatureService.ProcessWatermarkRemovalAsync(request.ImageData, request.Options);
            return Ok(new { imageData = processedData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing watermark removal");
            return StatusCode(500, new { message = "An error occurred while processing watermark removal" });
        }
    }

    [HttpPost("detect-watermark")]
    public async Task<ActionResult<WatermarkRemovalOptions>> DetectWatermark([FromBody] DetectWatermarkRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var options = await _signatureService.DetectWatermarkAsync(request.ImageData);
            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting watermark");
            return StatusCode(500, new { message = "An error occurred while detecting watermark" });
        }
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<byte[]>> OptimizeSignature([FromBody] OptimizeSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var optimizedData = await _signatureService.OptimizeSignatureAsync(request.SignatureData, request.OutputFormat);
            return Ok(new { signatureData = optimizedData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing signature");
            return StatusCode(500, new { message = "An error occurred while optimizing signature" });
        }
    }

    [HttpPost("validate-format")]
    public async Task<ActionResult<bool>> ValidateSignatureFormat([FromBody] ValidateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _signatureService.IsValidSignatureFormatAsync(request.SignatureData);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature format");
            return StatusCode(500, new { message = "An error occurred while validating signature format" });
        }
    }

    [HttpPost("validate-quality-acceptable")]
    public async Task<ActionResult<bool>> IsSignatureQualityAcceptable([FromBody] ValidateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isAcceptable = await _signatureService.IsSignatureQualityAcceptableAsync(request.SignatureData);
            return Ok(new { isAcceptable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature quality acceptability");
            return StatusCode(500, new { message = "An error occurred while validating signature quality" });
        }
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<Dictionary<string, object>>> AnalyzeSignature([FromBody] ValidateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var analysis = await _signatureService.AnalyzeSignatureAsync(request.SignatureData);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing signature");
            return StatusCode(500, new { message = "An error occurred while analyzing signature" });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("nameid")?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found in token");
    }
}

// Additional request DTOs for the SignatureController
public class RemoveWatermarkRequest
{
    [Required]
    public byte[] SignatureData { get; set; } = Array.Empty<byte>();
    
    [Required]
    public WatermarkRemovalOptions Options { get; set; } = new WatermarkRemovalOptions();
}

public class ProcessWatermarkRemovalRequest
{
    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    
    [Required]
    public WatermarkRemovalOptions Options { get; set; } = new WatermarkRemovalOptions();
}

public class DetectWatermarkRequest
{
    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
}

public class OptimizeSignatureRequest
{
    [Required]
    public byte[] SignatureData { get; set; } = Array.Empty<byte>();
    
    public string OutputFormat { get; set; } = "png";
}
