using DocHub.Application.Services;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Excel;
using DocHub.Shared.DTOs.Files;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExcelController : ControllerBase
{
    private readonly IExcelProcessingService _excelService;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<ExcelController> _logger;

    public ExcelController(IExcelProcessingService excelService, IDynamicTableService dynamicTableService, ILogger<ExcelController> logger)
    {
        _excelService = excelService;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<ExcelUploadDto>>> UploadExcel([FromForm] UploadExcelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ExcelUploadDto>.ErrorResult("Invalid request data"));
            }

            var userId = GetCurrentUserId();
            var upload = await _excelService.UploadExcelAsync(request, userId);
            return Ok(ApiResponse<ExcelUploadDto>.SuccessResult(upload));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ExcelUploadDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Excel file");
            return StatusCode(500, ApiResponse<ExcelUploadDto>.ErrorResult("An error occurred while uploading Excel file"));
        }
    }

    // Frontend-compatible endpoint for Excel upload by tab
    [HttpPost("tab/{tabId}")]
    public async Task<ActionResult<ApiResponse<FrontendExcelData>>> UploadExcelForTab(string tabId, [FromForm] IFormFile file, [FromForm] string? description, [FromForm] string? metadata)
    {
        try
        {
            _logger.LogInformation("🌐 [EXCEL-CONTROLLER] Received Excel upload request for tab {TabId}", tabId);
            _logger.LogInformation("📁 [EXCEL-CONTROLLER] File details: Name={FileName}, Size={FileSize}, ContentType={ContentType}", 
                file?.FileName, file?.Length, file?.ContentType);
            _logger.LogInformation("📝 [EXCEL-CONTROLLER] Additional data: Description={Description}, Metadata={Metadata}", 
                description, metadata);

            if (file == null || file.Length == 0)
            {
                _logger.LogError("❌ [EXCEL-CONTROLLER] No file provided or file is empty");
                return BadRequest(ApiResponse<FrontendExcelData>.ErrorResult("No file provided"));
            }

            if (!Guid.TryParse(tabId, out var tabGuid))
            {
                _logger.LogError("❌ [EXCEL-CONTROLLER] Invalid tab ID format: {TabId}", tabId);
                return BadRequest(ApiResponse<FrontendExcelData>.ErrorResult("Invalid tab ID"));
            }

            // Create UploadExcelRequest from form data
            _logger.LogInformation("📝 [EXCEL-CONTROLLER] Creating UploadExcelRequest...");
            var request = new UploadExcelRequest
            {
                File = file,
                Category = "excel", // Required field
                SubCategory = "upload",
                Description = description ?? string.Empty,
                LetterTypeDefinitionId = tabGuid, // Map tabId to LetterTypeDefinitionId
                Metadata = metadata
            };

            var userId = GetCurrentUserId();
            _logger.LogInformation("👤 [EXCEL-CONTROLLER] Current user ID: {UserId}", userId);
            
            _logger.LogInformation("🔄 [EXCEL-CONTROLLER] Calling ExcelService.UploadExcelAsync...");
            var upload = await _excelService.UploadExcelAsync(request, userId);
            _logger.LogInformation("✅ [EXCEL-CONTROLLER] ExcelService.UploadExcelAsync completed successfully");
            
            // Process the Excel data and import into dynamic table
            _logger.LogInformation("🔄 [EXCEL-CONTROLLER] Processing Excel data and importing into dynamic table...");
            try
            {
                var importSuccess = await _excelService.ImportExcelDataAsync(upload.Id, tabGuid, "{}", userId);
                _logger.LogInformation("✅ [EXCEL-CONTROLLER] Excel data import completed: {Success}", importSuccess);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No dynamic table found"))
            {
                _logger.LogWarning("⚠️ [EXCEL-CONTROLLER] Dynamic table not found for tab {TabId}, skipping import: {Message}", tabGuid, ex.Message);
                // Continue without throwing - the upload was successful, just no table to import into yet
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [EXCEL-CONTROLLER] Error importing Excel data: {Message}", ex.Message);
                // Continue without throwing - the upload was successful, just import failed
            }
            
            // Convert to frontend format
            _logger.LogInformation("🔄 [EXCEL-CONTROLLER] Converting to frontend format...");
            var frontendData = await ConvertToFrontendExcelData(upload);
            _logger.LogInformation("✅ [EXCEL-CONTROLLER] Frontend conversion completed");
            
            _logger.LogInformation("🎉 [EXCEL-CONTROLLER] Returning success response");
            return Ok(ApiResponse<FrontendExcelData>.SuccessResult(frontendData));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("❌ [EXCEL-CONTROLLER] ArgumentException: {Message}", ex.Message);
            return BadRequest(ApiResponse<FrontendExcelData>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-CONTROLLER] Error uploading Excel file for tab {TabId}: {Message}", tabId, ex.Message);
            return StatusCode(500, ApiResponse<FrontendExcelData>.ErrorResult("An error occurred while uploading Excel file"));
        }
    }

    // Frontend-compatible endpoint to get Excel data for a tab
    [HttpGet("tab/{tabId}")]
    public async Task<ActionResult<ApiResponse<FrontendExcelData>>> GetExcelDataForTab(string tabId)
    {
        try
        {
            if (!Guid.TryParse(tabId, out var tabGuid))
            {
                return BadRequest(ApiResponse<FrontendExcelData>.ErrorResult("Invalid tab ID"));
            }

            // For now, return mock data structure
            // In a real implementation, you'd query ExcelUploads by LetterTypeDefinitionId
            var uploads = await _excelService.GetExcelUploadsAsync();
            var tabUpload = uploads.FirstOrDefault(u => u.LetterTypeDefinitionId == tabGuid);

            if (tabUpload == null)
            {
                return Ok(ApiResponse<FrontendExcelData>.SuccessResult(new FrontendExcelData
                {
                    Id = Guid.Empty,
                    Headers = new List<string>(),
                    Data = new List<Dictionary<string, object>>(),
                    FileName = "",
                    FileSize = 0,
                    UploadedAt = DateTime.UtcNow
                }));
            }

            var frontendData = await ConvertToFrontendExcelData(tabUpload);
            return Ok(ApiResponse<FrontendExcelData>.SuccessResult(frontendData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel data for tab {TabId}", tabId);
            return StatusCode(500, ApiResponse<FrontendExcelData>.ErrorResult("An error occurred while getting Excel data"));
        }
    }

    [HttpPost("upload-frontend")]
    public async Task<ActionResult<FrontendExcelUploadResponse>> UploadExcelFrontend([FromForm] UploadExcelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Ok(new FrontendExcelUploadResponse 
                { 
                    Success = false, 
                    Error = "Invalid request data" 
                });
            }

            var userId = GetCurrentUserId();
            var upload = await _excelService.UploadExcelAsync(request, userId);
            
            // Convert to frontend-compatible response
            var frontendData = await ConvertToFrontendExcelData(upload);
            
            return Ok(new FrontendExcelUploadResponse 
            { 
                Success = true, 
                Data = frontendData 
            });
        }
        catch (ArgumentException ex)
        {
            return Ok(new FrontendExcelUploadResponse 
            { 
                Success = false, 
                Error = ex.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Excel file");
            return Ok(new FrontendExcelUploadResponse 
            { 
                Success = false, 
                Error = "An error occurred while uploading Excel file" 
            });
        }
    }

    [HttpPost("process/{uploadId}")]
    public async Task<ActionResult<ExcelProcessingResultDto>> ProcessExcel(Guid uploadId, [FromBody] ProcessExcelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var result = await _excelService.ProcessExcelAsync(uploadId, request, userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file {UploadId}", uploadId);
            return StatusCode(500, new { message = "An error occurred while processing Excel file" });
        }
    }

    [HttpGet("uploads")]
    public async Task<ActionResult<IEnumerable<ExcelUploadDto>>> GetExcelUploads([FromQuery] Guid? letterTypeId = null)
    {
        try
        {
            var uploads = await _excelService.GetExcelUploadsAsync(letterTypeId);
            return Ok(uploads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel uploads");
            return StatusCode(500, new { message = "An error occurred while getting Excel uploads" });
        }
    }

    [HttpGet("uploads/{id}")]
    public async Task<ActionResult<ExcelUploadDto>> GetExcelUpload(Guid id)
    {
        try
        {
            var upload = await _excelService.GetExcelUploadAsync(id);
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

    [HttpDelete("uploads/{id}")]
    public async Task<ActionResult> DeleteExcelUpload(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _excelService.DeleteExcelUploadAsync(id, userId);
            return Ok(new { message = "Excel upload deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Excel upload {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting Excel upload" });
        }
    }

    [HttpGet("uploads/{id}/preview")]
    public async Task<ActionResult<ExcelPreviewDto>> PreviewExcel(Guid id, [FromQuery] int maxRows = 100)
    {
        try
        {
            var preview = await _excelService.PreviewExcelAsync(id, maxRows);
            return Ok(preview);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing Excel file {Id}", id);
            return StatusCode(500, new { message = "An error occurred while previewing Excel file" });
        }
    }

    [HttpGet("uploads/{id}/headers")]
    public async Task<ActionResult<IEnumerable<string>>> GetExcelHeaders(Guid id)
    {
        try
        {
            var headers = await _excelService.GetExcelHeadersAsync(id);
            return Ok(headers);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel headers for upload {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting Excel headers" });
        }
    }

    [HttpPost("uploads/{id}/validate")]
    public async Task<ActionResult<ExcelValidationResultDto>> ValidateExcel(Guid id, [FromBody] ValidateExcelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _excelService.ValidateExcelAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel file {Id}", id);
            return StatusCode(500, new { message = "An error occurred while validating Excel file" });
        }
    }

    [HttpPost("uploads/{id}/map-fields")]
    public async Task<ActionResult<FieldMappingResultDto>> MapFields(Guid id, [FromBody] MapFieldsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _excelService.MapFieldsAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping fields for Excel upload {Id}", id);
            return StatusCode(500, new { message = "An error occurred while mapping fields" });
        }
    }

    [HttpPost("uploads/{id}/import")]
    public async Task<ActionResult<ImportResultDto>> ImportData(Guid id, [FromBody] ImportDataRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var result = await _excelService.ImportDataAsync(id, request, userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data from Excel upload {Id}", id);
            return StatusCode(500, new { message = "An error occurred while importing data" });
        }
    }

    [HttpGet("uploads/{id}/download")]
    public async Task<IActionResult> DownloadExcel(Guid id, [FromQuery] string format = "xlsx")
    {
        try
        {
            var stream = await _excelService.DownloadExcelAsync(id, format);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"excel_{id}.{format}");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading Excel file {Id}", id);
            return StatusCode(500, new { message = "An error occurred while downloading Excel file" });
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<ExcelTemplateDto>>> GetExcelTemplates([FromQuery] Guid? letterTypeId = null)
    {
        try
        {
            var templates = await _excelService.GetExcelTemplatesAsync(letterTypeId);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel templates");
            return StatusCode(500, new { message = "An error occurred while getting Excel templates" });
        }
    }

    [HttpPost("templates")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExcelTemplateDto>> CreateExcelTemplate([FromBody] CreateExcelTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var template = await _excelService.CreateExcelTemplateAsync(request, userId);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Excel template");
            return StatusCode(500, new { message = "An error occurred while creating Excel template" });
        }
    }

    [HttpPut("templates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExcelTemplateDto>> UpdateExcelTemplate(Guid id, [FromBody] UpdateExcelTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var template = await _excelService.UpdateExcelTemplateAsync(id, request, userId);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Excel template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating Excel template" });
        }
    }

    [HttpDelete("templates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteExcelTemplate(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _excelService.DeleteExcelTemplateAsync(id, userId);
            return Ok(new { message = "Excel template deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Excel template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting Excel template" });
        }
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<ExcelAnalyticsDto>> GetExcelAnalytics([FromQuery] Guid? letterTypeId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var analytics = await _excelService.GetExcelAnalyticsAsync(letterTypeId, fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel analytics");
            return StatusCode(500, new { message = "An error occurred while getting Excel analytics" });
        }
    }

    private async Task<FrontendExcelData> ConvertToFrontendExcelData(ExcelUploadDto upload)
    {
        var headers = new List<string>();
        var data = new List<Dictionary<string, object>>();

        try
        {
            // Try to get data from dynamic table first
            var tableName = await _dynamicTableService.GetTableNameForLetterTypeAsync(upload.LetterTypeDefinitionId);
            if (!string.IsNullOrEmpty(tableName))
            {
                _logger.LogInformation("📊 [EXCEL-CONTROLLER] Getting data from dynamic table: {TableName}", tableName);
                var tableData = await _dynamicTableService.GetDataFromDynamicTableAsync(tableName, 0, 1000);
                
                if (tableData.Any())
                {
                    // Extract headers from the first row
                    headers = tableData.First().Keys.ToList();
                    data = tableData;
                    _logger.LogInformation("✅ [EXCEL-CONTROLLER] Retrieved {RowCount} rows from dynamic table", data.Count);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ [EXCEL-CONTROLLER] No dynamic table found for letter type {LetterTypeId}", upload.LetterTypeDefinitionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-CONTROLLER] Error getting data from dynamic table for upload {UploadId}", upload.Id);
            
            // Fallback to parsing ParsedData if dynamic table fails
            if (!string.IsNullOrEmpty(upload.ParsedData))
            {
                try
                {
                    var parsedData = JsonSerializer.Deserialize<Dictionary<string, object>>(upload.ParsedData);
                    if (parsedData != null && parsedData.ContainsKey("TableName"))
                    {
                        _logger.LogInformation("🔄 [EXCEL-CONTROLLER] Fallback: Using table name from ParsedData: {TableName}", parsedData["TableName"]);
                        // This is the new format, data is already in the dynamic table
                        // Return empty data as the table exists but we couldn't query it
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning(fallbackEx, "Error parsing fallback Excel data for upload {UploadId}", upload.Id);
                }
            }
        }

        return new FrontendExcelData
        {
            Id = upload.Id,
            Headers = headers,
            Data = data,
            FileName = upload.FileName,
            FileSize = upload.FileSize,
            UploadedAt = upload.CreatedAt
        };
    }

    [HttpGet("data/{uploadId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetExcelData(Guid uploadId, int skip = 0, int take = 100)
    {
        try
        {
            _logger.LogInformation("🔍 [EXCEL-CONTROLLER] Getting Excel data for upload {UploadId}", uploadId);

            // Get the upload to find the table name
            var upload = await _excelService.GetExcelUploadAsync(uploadId);
            if (upload == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Excel upload not found"));
            }

            // Parse the ParsedData to get table name
            if (string.IsNullOrEmpty(upload.ParsedData))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Excel data not processed yet"));
            }

            var parsedData = JsonSerializer.Deserialize<Dictionary<string, object>>(upload.ParsedData);
            if (parsedData == null || !parsedData.ContainsKey("TableName"))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid parsed data format"));
            }

            var tableName = parsedData["TableName"].ToString();
            if (string.IsNullOrEmpty(tableName))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Table name not found"));
            }

            // Get data from dynamic table
            var data = await _excelService.GetDataFromDynamicTableAsync(tableName, skip, take);
            
            _logger.LogInformation("✅ [EXCEL-CONTROLLER] Retrieved {RowCount} rows from table {TableName}", data.Count, tableName);

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                TableName = tableName,
                Data = data,
                TotalRows = upload.ProcessedRows,
                Skip = skip,
                Take = take
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-CONTROLLER] Error getting Excel data for upload {UploadId}", uploadId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error"));
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
