using DocHub.Application.Services;
using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using DocHub.Shared.DTOs.Files;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly DocumentGenerationService _documentService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(DocumentGenerationService documentService, ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<GeneratedDocumentDto>>> GenerateDocument([FromBody] GenerateDocumentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<GeneratedDocumentDto>.ErrorResult("Invalid request data"));
            }

            var userId = GetCurrentUserId();
            var document = await _documentService.GenerateDocumentAsync(request, userId);
            return Ok(ApiResponse<GeneratedDocumentDto>.SuccessResult(document));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<GeneratedDocumentDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document");
            return StatusCode(500, ApiResponse<GeneratedDocumentDto>.ErrorResult("An error occurred while generating document"));
        }
    }

    [HttpPost("generate-frontend")]
    public async Task<ActionResult<ApiResponse<GeneratedDocumentDto>>> GenerateDocumentFrontend([FromBody] FrontendGenerateDocumentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<GeneratedDocumentDto>.ErrorResult("Invalid request data"));
            }

            var userId = GetCurrentUserId();

            // Convert frontend request to backend request
            var backendRequest = await ConvertFrontendRequest(request, userId);

            var document = await _documentService.GenerateDocumentAsync(backendRequest, userId);
            return Ok(ApiResponse<GeneratedDocumentDto>.SuccessResult(document));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<GeneratedDocumentDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document from frontend request");
            return StatusCode(500, ApiResponse<GeneratedDocumentDto>.ErrorResult("An error occurred while generating document"));
        }
    }

    [HttpPost("generate-bulk")]
    public async Task<ActionResult<IEnumerable<GeneratedDocumentDto>>> GenerateBulkDocuments([FromBody] GenerateBulkDocumentsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var documents = await _documentService.GenerateBulkDocumentsAsync(request, userId);
            return Ok(documents);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulk documents");
            return StatusCode(500, new { message = "An error occurred while generating bulk documents" });
        }
    }

    [HttpPost("preview")]
    public async Task<ActionResult<DocumentPreviewDto>> PreviewDocument([FromBody] PreviewDocumentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var preview = await _documentService.PreviewDocumentAsync(request, userId);
            return Ok(preview);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing document");
            return StatusCode(500, new { message = "An error occurred while previewing document" });
        }
    }

    [HttpGet("{documentId}/download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId, [FromQuery] string format = "docx")
    {
        try
        {
            var stream = await _documentService.DownloadDocumentAsync(documentId, format);
            return File(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"document_{documentId}.{format}");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            return StatusCode(500, new { message = "An error occurred while downloading document" });
        }
    }

    [HttpPost("process-template")]
    public async Task<ActionResult<DocumentTemplateDto>> ProcessTemplate([FromBody] ProcessTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var template = await _documentService.ProcessTemplateAsync(request, userId);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template");
            return StatusCode(500, new { message = "An error occurred while processing template" });
        }
    }

    [HttpGet("templates/{templateId}/placeholders")]
    public async Task<ActionResult<IEnumerable<string>>> GetPlaceholders(Guid templateId)
    {
        try
        {
            var placeholders = await _documentService.ExtractPlaceholdersAsync(templateId);
            return Ok(placeholders);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting placeholders from template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred while extracting placeholders" });
        }
    }

    [HttpGet("templates/{templateId}/validate")]
    public async Task<ActionResult<bool>> ValidateTemplate(Guid templateId)
    {
        try
        {
            var isValid = await _documentService.ValidateTemplateAsync(templateId);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred while validating template" });
        }
    }

    [HttpPost("insert-signature")]
    public async Task<ActionResult<GeneratedDocumentDto>> InsertSignature([FromBody] InsertSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var document = await _documentService.InsertSignatureIntoDocumentAsync(request, userId);
            return Ok(document);
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

    // Signature processing is handled by SignatureController
    // This endpoint has been moved to /api/signature/process

    [HttpPost("validate-signature")]
    public async Task<ActionResult<bool>> ValidateSignature([FromBody] ValidateSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _documentService.ValidateSignatureQualityAsync(request.SignatureData);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature");
            return StatusCode(500, new { message = "An error occurred while validating signature" });
        }
    }

    private Task<GenerateDocumentRequest> ConvertFrontendRequest(FrontendGenerateDocumentRequest request, string userId)
    {
        // For now, we'll assume the employeeId maps to a TabDataRecord
        // In a real implementation, you would look up the employee and find the corresponding TabDataRecord
        // and determine the LetterTypeDefinition based on the template

        // This is a simplified mapping - in reality, you'd need to:
        // 1. Find the TabDataRecord by employeeId
        // 2. Determine the LetterTypeDefinition from the template
        // 3. Convert placeholderData to ProcessingOptions JSON

        return Task.FromResult(new GenerateDocumentRequest
        {
            LetterTypeDefinitionId = Guid.NewGuid(), // This should be determined from the template
            ExcelUploadId = request.EmployeeId, // Assuming employeeId maps to ExcelUploadId
            TemplateId = request.TemplateId,
            SignatureId = request.SignatureId,
            ProcessingOptions = JsonSerializer.Serialize(request.PlaceholderData)
        });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("nameid")?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found in token");
    }
}
