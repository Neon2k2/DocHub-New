using DocHub.Application.Services;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Files;
using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileManagementService _fileService;
    private readonly ILogger<FileController> _logger;

    public FileController(IFileManagementService fileService, ILogger<FileController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileReferenceDto>> UploadFile([FromForm] UploadFileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var fileReference = await _fileService.UploadFileAsync(request, userId);
            return Ok(fileReference);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "An error occurred while uploading file" });
        }
    }

    [HttpGet("{fileId}/download")]
    public async Task<IActionResult> DownloadFile(Guid fileId)
    {
        try
        {
            var stream = await _fileService.DownloadFileAsync(fileId);
            var fileInfo = await _fileService.GetFileInfoAsync(fileId);
            
            return File(stream, fileInfo.MimeType, fileInfo.FileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return StatusCode(500, new { message = "An error occurred while downloading file" });
        }
    }

    [HttpDelete("{fileId}")]
    public async Task<ActionResult> DeleteFile(Guid fileId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _fileService.DeleteFileAsync(fileId, userId);
            return Ok(new { message = "File deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", fileId);
            return StatusCode(500, new { message = "An error occurred while deleting file" });
        }
    }

    [HttpGet("{fileId}")]
    public async Task<ActionResult<FileReferenceDto>> GetFileInfo(Guid fileId)
    {
        try
        {
            var fileInfo = await _fileService.GetFileInfoAsync(fileId);
            return Ok(fileInfo);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info {FileId}", fileId);
            return StatusCode(500, new { message = "An error occurred while getting file info" });
        }
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<FileReferenceDto>>> GetFilesByCategory(string category)
    {
        try
        {
            var files = await _fileService.GetFilesByCategoryAsync(category);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files by category {Category}", category);
            return StatusCode(500, new { message = "An error occurred while getting files" });
        }
    }

    [HttpPost("templates")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DocumentTemplateDto>> UploadTemplate([FromForm] UploadTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var template = await _fileService.UploadTemplateAsync(request, userId);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template");
            return StatusCode(500, new { message = "An error occurred while uploading template" });
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplates()
    {
        try
        {
            var templates = await _fileService.GetTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            return StatusCode(500, new { message = "An error occurred while getting templates" });
        }
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<DocumentTemplateDto>> GetTemplate(Guid id)
    {
        try
        {
            var template = await _fileService.GetTemplateAsync(id);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting template" });
        }
    }

    [HttpDelete("templates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _fileService.DeleteTemplateAsync(id, userId);
            return Ok(new { message = "Template deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting template" });
        }
    }

    [HttpGet("templates/{id}/placeholders")]
    public async Task<ActionResult<IEnumerable<string>>> GetTemplatePlaceholders(Guid id)
    {
        try
        {
            var placeholders = await _fileService.ExtractPlaceholdersAsync(id);
            return Ok(placeholders);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting placeholders from template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while extracting placeholders" });
        }
    }

    [HttpPost("signatures")]
    public async Task<ActionResult<SignatureDto>> UploadSignature([FromForm] UploadSignatureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var signature = await _fileService.UploadSignatureAsync(request, userId);
            return Ok(signature);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading signature");
            return StatusCode(500, new { message = "An error occurred while uploading signature" });
        }
    }

    [HttpGet("signatures")]
    public async Task<ActionResult<IEnumerable<SignatureDto>>> GetSignatures()
    {
        try
        {
            var signatures = await _fileService.GetSignaturesAsync();
            return Ok(signatures);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signatures");
            return StatusCode(500, new { message = "An error occurred while getting signatures" });
        }
    }

    [HttpGet("signatures/{id}")]
    public async Task<ActionResult<SignatureDto>> GetSignature(Guid id)
    {
        try
        {
            var signature = await _fileService.GetSignatureAsync(id);
            return Ok(signature);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting signature" });
        }
    }

    [HttpDelete("signatures/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteSignature(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _fileService.DeleteSignatureAsync(id, userId);
            return Ok(new { message = "Signature deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting signature {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting signature" });
        }
    }

    [HttpPost("excel")]
    public async Task<ActionResult<ExcelUploadDto>> UploadExcel([FromForm] UploadExcelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var excelUpload = await _fileService.UploadExcelAsync(request, userId);
            return Ok(excelUpload);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Excel file");
            return StatusCode(500, new { message = "An error occurred while uploading Excel file" });
        }
    }

    [HttpGet("excel")]
    public async Task<ActionResult<IEnumerable<ExcelUploadDto>>> GetExcelUploads([FromQuery] Guid? letterTypeId = null)
    {
        try
        {
            var uploads = await _fileService.GetExcelUploadsAsync(letterTypeId);
            return Ok(uploads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel uploads");
            return StatusCode(500, new { message = "An error occurred while getting Excel uploads" });
        }
    }

    [HttpGet("excel/{id}")]
    public async Task<ActionResult<ExcelUploadDto>> GetExcelUpload(Guid id)
    {
        try
        {
            var upload = await _fileService.GetExcelUploadAsync(id);
            return Ok(upload);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel upload {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting Excel upload" });
        }
    }

    [HttpPost("{fileId}/process")]
    public async Task<ActionResult<byte[]>> ProcessFile(Guid fileId, [FromBody] ProcessFileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var processedData = await _fileService.ProcessFileAsync(fileId, request.ProcessingOptions ?? string.Empty);
            return Ok(new { data = processedData });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FileId}", fileId);
            return StatusCode(500, new { message = "An error occurred while processing file" });
        }
    }

    [HttpPost("validate")]
    public async Task<ActionResult<bool>> ValidateFile([FromForm] IFormFile file, [FromQuery] string category)
    {
        try
        {
            var isValid = await _fileService.ValidateFileAsync(file, category);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file");
            return StatusCode(500, new { message = "An error occurred while validating file" });
        }
    }

    [HttpPost("hash")]
    public async Task<ActionResult<string>> GenerateFileHash([FromForm] IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var hash = await _fileService.GenerateFileHashAsync(stream);
            return Ok(new { hash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating file hash");
            return StatusCode(500, new { message = "An error occurred while generating file hash" });
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
