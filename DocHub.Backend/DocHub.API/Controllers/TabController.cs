using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Tabs;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TabController : ControllerBase
{
    private readonly ITabManagementService _tabService;
    private readonly IDynamicLetterGenerationService _letterGenerationService;
    private readonly ITemplateService _templateService;
    private readonly IRepository<TableSchema> _tableSchemaRepository;
    private readonly IDbContext _dbContext;
    private readonly ILogger<TabController> _logger;

    public TabController(ITabManagementService tabService, IDynamicLetterGenerationService letterGenerationService, ITemplateService templateService, IRepository<TableSchema> tableSchemaRepository, IDbContext dbContext, ILogger<TabController> logger)
    {
        _tabService = tabService;
        _letterGenerationService = letterGenerationService;
        _templateService = templateService;
        _tableSchemaRepository = tableSchemaRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    // Letter Type Management Endpoints

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LetterTypeDefinitionDto>>>> GetLetterTypes()
    {
        try
        {
            var letterTypes = await _tabService.GetLetterTypesAsync();
            return Ok(ApiResponse<IEnumerable<LetterTypeDefinitionDto>>.SuccessResult(letterTypes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting letter types");
            return StatusCode(500, ApiResponse<IEnumerable<LetterTypeDefinitionDto>>.ErrorResult("An error occurred while getting letter types"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinitionDto>>> GetLetterType(Guid id)
    {
        try
        {
            var letterType = await _tabService.GetLetterTypeAsync(id);
            return Ok(ApiResponse<LetterTypeDefinitionDto>.SuccessResult(letterType));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<LetterTypeDefinitionDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting letter type {Id}", id);
            return StatusCode(500, ApiResponse<LetterTypeDefinitionDto>.ErrorResult("An error occurred while getting letter type"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinitionDto>>> CreateLetterType([FromBody] JsonElement requestData)
    {
        try
        {
            _logger.LogInformation("🚀 [TAB-CREATE] Starting tab creation process");
            _logger.LogInformation("📥 [TAB-CREATE] Received request data: {RequestData}", requestData.GetRawText());
            
            if (requestData.ValueKind == JsonValueKind.Undefined || requestData.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("❌ [TAB-CREATE] Request data is null or undefined");
                return BadRequest(ApiResponse<LetterTypeDefinitionDto>.ErrorResult("Invalid request data"));
            }

            _logger.LogInformation("🔄 [TAB-CREATE] Deserializing frontend request data...");
            // Convert from frontend format (no module required)
            var frontendData = JsonSerializer.Deserialize<FrontendCreateLetterTypeRequest>(requestData.GetRawText());
            if (frontendData == null)
            {
                _logger.LogError("❌ [TAB-CREATE] Failed to deserialize frontend request data");
                return BadRequest(ApiResponse<LetterTypeDefinitionDto>.ErrorResult("Invalid request format"));
            }
            
            _logger.LogInformation("✅ [TAB-CREATE] Frontend data deserialized successfully");
            _logger.LogInformation("📋 [TAB-CREATE] Data details - TypeKey: {TypeKey}, DisplayName: {DisplayName}, DataSourceType: {DataSourceType}", 
                frontendData.TypeKey, frontendData.DisplayName, frontendData.DataSourceType);
            
            _logger.LogInformation("🔧 [TAB-CREATE] Creating backend request object...");
            // Create request without module requirement
            var request = new CreateLetterTypeRequest
            {
                TypeKey = frontendData.TypeKey,
                DisplayName = frontendData.DisplayName,
                Description = frontendData.Description,
                DataSourceType = frontendData.DataSourceType,
                FieldConfiguration = frontendData.FieldConfiguration,
                // ModuleId removed - no module dependency
            };

            _logger.LogInformation("✅ [TAB-CREATE] Backend request object created");
            _logger.LogInformation("📊 [TAB-CREATE] Request details - TypeKey: {TypeKey}, DisplayName: {DisplayName}", 
                request.TypeKey, request.DisplayName);

            _logger.LogInformation("🔍 [TAB-CREATE] Validating model state...");
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("❌ [TAB-CREATE] Model validation failed: {Errors}", errors);
                return BadRequest(ApiResponse<LetterTypeDefinitionDto>.ErrorResult($"Invalid request data: {errors}"));
            }
            _logger.LogInformation("✅ [TAB-CREATE] Model validation passed");

            _logger.LogInformation("👤 [TAB-CREATE] Getting current user ID...");
            var userId = GetCurrentUserId();
            _logger.LogInformation("✅ [TAB-CREATE] User ID retrieved: {UserId}", userId);

            _logger.LogInformation("🔄 [TAB-CREATE] Calling TabManagementService.CreateLetterTypeAsync...");
            var letterType = await _tabService.CreateLetterTypeAsync(request, userId);
            _logger.LogInformation("✅ [TAB-CREATE] TabManagementService completed successfully");
            _logger.LogInformation("📄 [TAB-CREATE] Created letter type ID: {LetterTypeId}", letterType.Id);

            _logger.LogInformation("🎉 [TAB-CREATE] Tab creation process completed successfully");
            return Ok(ApiResponse<LetterTypeDefinitionDto>.SuccessResult(letterType));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "❌ [TAB-CREATE] Argument exception in CreateLetterType: {Message}", ex.Message);
            return BadRequest(ApiResponse<LetterTypeDefinitionDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TAB-CREATE] Unexpected error creating letter type: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<LetterTypeDefinitionDto>.ErrorResult("An error occurred while creating letter type"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinitionDto>>> UpdateLetterType(Guid id, [FromBody] UpdateLetterTypeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LetterTypeDefinitionDto>.ErrorResult("Invalid request data"));
            }

            var userId = GetCurrentUserId();
            var letterType = await _tabService.UpdateLetterTypeAsync(id, request, userId);
            return Ok(ApiResponse<LetterTypeDefinitionDto>.SuccessResult(letterType));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<LetterTypeDefinitionDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating letter type {Id}", id);
            return StatusCode(500, ApiResponse<LetterTypeDefinitionDto>.ErrorResult("An error occurred while updating letter type"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLetterType(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _tabService.DeleteLetterTypeAsync(id, userId);
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Letter type deleted successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting letter type {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting letter type"));
        }
    }

    // Dynamic Field Management Endpoints

    [HttpGet("{letterTypeId}/fields")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DynamicFieldDto>>>> GetFields(Guid letterTypeId)
    {
        try
        {
            var fields = await _tabService.GetFieldsAsync(letterTypeId);
            return Ok(ApiResponse<IEnumerable<DynamicFieldDto>>.SuccessResult(fields));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fields for letter type {LetterTypeId}", letterTypeId);
            return StatusCode(500, ApiResponse<IEnumerable<DynamicFieldDto>>.ErrorResult("An error occurred while getting fields"));
        }
    }

    [HttpPost("{letterTypeId}/fields")]
    public async Task<ActionResult<ApiResponse<DynamicFieldDto>>> CreateField(Guid letterTypeId, [FromBody] CreateDynamicFieldRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DynamicFieldDto>.ErrorResult("Invalid request data"));
            }

            // Ensure the letter type ID matches
            request.LetterTypeDefinitionId = letterTypeId;

            var userId = GetCurrentUserId();
            var field = await _tabService.CreateFieldAsync(request, userId);
            return Ok(ApiResponse<DynamicFieldDto>.SuccessResult(field));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DynamicFieldDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating field for letter type {LetterTypeId}", letterTypeId);
            return StatusCode(500, ApiResponse<DynamicFieldDto>.ErrorResult("An error occurred while creating field"));
        }
    }

    [HttpPut("{letterTypeId}/fields/{fieldId}")]
    public async Task<ActionResult<ApiResponse<DynamicFieldDto>>> UpdateField(Guid letterTypeId, Guid fieldId, [FromBody] UpdateDynamicFieldRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DynamicFieldDto>.ErrorResult("Invalid request data"));
            }

            var userId = GetCurrentUserId();
            var field = await _tabService.UpdateFieldAsync(fieldId, request, userId);
            return Ok(ApiResponse<DynamicFieldDto>.SuccessResult(field));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<DynamicFieldDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating field {FieldId}", fieldId);
            return StatusCode(500, ApiResponse<DynamicFieldDto>.ErrorResult("An error occurred while updating field"));
        }
    }

    [HttpDelete("{letterTypeId}/fields/{fieldId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteField(Guid letterTypeId, Guid fieldId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _tabService.DeleteFieldAsync(fieldId, userId);
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Field deleted successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting field {FieldId}", fieldId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting field"));
        }
    }

    // Tab Data Management Endpoints - Using dynamic tables

    [HttpGet("{id}/data")]
    public async Task<ActionResult<ApiResponse<object>>> GetTabData(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("🔍 [TAB-DATA] Getting data for tab {TabId}, page {Page}, pageSize {PageSize}", id, page, pageSize);
            
            // Get the letter type to find the table name
            var letterType = await _tabService.GetLetterTypeAsync(id);
            if (letterType == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Tab not found"));
            }

            // For now, return empty data structure since we need to implement dynamic table data retrieval
            // This matches what the frontend expects
            var result = new
            {
                Data = new List<Dictionary<string, object>>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                TotalPages = 0
            };

            _logger.LogInformation("✅ [TAB-DATA] Returning empty data structure for tab {TabId}", id);
            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "❌ [TAB-DATA] Argument exception getting data for tab {TabId}: {Message}", id, ex.Message);
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TAB-DATA] Error getting data for tab {TabId}: {Message}", id, ex.Message);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while getting tab data"));
        }
    }

    [HttpGet("{id}/statistics")]
    public async Task<ActionResult<ApiResponse<object>>> GetTabStatistics(Guid id)
    {
        try
        {
            _logger.LogInformation("📊 [TAB-STATS] Getting statistics for tab {TabId}", id);
            
            // Get the letter type to find the table name
            var letterType = await _tabService.GetLetterTypeAsync(id);
            if (letterType == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Tab not found"));
            }

            // For now, return empty statistics since we need to implement dynamic table data retrieval
            var result = new
            {
                TotalRecords = 0,
                LastUpdated = DateTime.UtcNow,
                DataSource = letterType.DataSourceType
            };

            _logger.LogInformation("✅ [TAB-STATS] Returning empty statistics for tab {TabId}", id);
            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "❌ [TAB-STATS] Argument exception getting statistics for tab {TabId}: {Message}", id, ex.Message);
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TAB-STATS] Error getting statistics for tab {TabId}: {Message}", id, ex.Message);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while getting tab statistics"));
        }
    }

    // Frontend-compatible endpoints

    [HttpGet("dynamic-tabs")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DynamicTabDto>>>> GetDynamicTabs()
    {
        try
        {
            var letterTypes = await _tabService.GetLetterTypesAsync();
            var dynamicTabs = letterTypes.Select(ConvertToFrontendDto);
            return Ok(ApiResponse<IEnumerable<DynamicTabDto>>.SuccessResult(dynamicTabs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dynamic tabs");
            return StatusCode(500, ApiResponse<IEnumerable<DynamicTabDto>>.ErrorResult("An error occurred while getting dynamic tabs"));
        }
    }

    [HttpPost("dynamic-tabs")]
    public async Task<ActionResult<ApiResponse<DynamicTabDto>>> CreateDynamicTab([FromBody] CreateDynamicTabRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DynamicTabDto>.ErrorResult("Invalid request data"));
            }

            // Convert frontend request to backend request
            var backendRequest = new CreateLetterTypeRequest
            {
                TypeKey = request.Name.Replace(" ", "_").ToLower(),
                DisplayName = request.DisplayName,
                Description = request.Description,
                DataSourceType = request.DataSource == "excel" ? "Excel" : "Database",
                // ModuleId removed - no module dependency
                Fields = request.Fields.Select(f => new CreateDynamicFieldRequest
                {
                    FieldKey = f.Name.Replace(" ", "_").ToLower(),
                    FieldName = f.Name,
                    DisplayName = f.DisplayName,
                    FieldType = f.Type,
                    IsRequired = f.Required,
                    OrderIndex = f.Order,
                    ValidationRules = f.Validation != null ? JsonSerializer.Serialize(f.Validation) : null
                }).ToList()
            };

            var userId = GetCurrentUserId();
            var letterType = await _tabService.CreateLetterTypeAsync(backendRequest, userId);
            var dynamicTab = ConvertToFrontendDto(letterType);
            
            return Ok(ApiResponse<DynamicTabDto>.SuccessResult(dynamicTab));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DynamicTabDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dynamic tab");
            return StatusCode(500, ApiResponse<DynamicTabDto>.ErrorResult("An error occurred while creating dynamic tab"));
        }
    }

    [HttpGet("dynamic-tabs/{id}")]
    public async Task<ActionResult<ApiResponse<DynamicTabDto>>> GetDynamicTab(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var tabId))
            {
                return BadRequest(ApiResponse<DynamicTabDto>.ErrorResult("Invalid tab ID"));
            }

            var letterType = await _tabService.GetLetterTypeAsync(tabId);
            var dynamicTab = ConvertToFrontendDto(letterType);
            return Ok(ApiResponse<DynamicTabDto>.SuccessResult(dynamicTab));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<DynamicTabDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dynamic tab {Id}", id);
            return StatusCode(500, ApiResponse<DynamicTabDto>.ErrorResult("An error occurred while getting dynamic tab"));
        }
    }

    [HttpPut("dynamic-tabs/{id}")]
    public async Task<ActionResult<ApiResponse<DynamicTabDto>>> UpdateDynamicTab(string id, [FromBody] UpdateDynamicTabRequest request)
    {
        try
        {
            if (!Guid.TryParse(id, out var tabId))
            {
                return BadRequest(ApiResponse<DynamicTabDto>.ErrorResult("Invalid tab ID"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DynamicTabDto>.ErrorResult("Invalid request data"));
            }

            // Convert frontend request to backend request
            var backendRequest = new UpdateLetterTypeRequest
            {
                DisplayName = request.DisplayName,
                Description = request.Description,
                IsActive = request.IsActive
            };

            var userId = GetCurrentUserId();
            var letterType = await _tabService.UpdateLetterTypeAsync(tabId, backendRequest, userId);
            var dynamicTab = ConvertToFrontendDto(letterType);
            
            return Ok(ApiResponse<DynamicTabDto>.SuccessResult(dynamicTab));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<DynamicTabDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dynamic tab {Id}", id);
            return StatusCode(500, ApiResponse<DynamicTabDto>.ErrorResult("An error occurred while updating dynamic tab"));
        }
    }

    [HttpDelete("dynamic-tabs/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDynamicTab(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var tabId))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tab ID"));
            }

            var userId = GetCurrentUserId();
            await _tabService.DeleteLetterTypeAsync(tabId, userId);
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Dynamic tab deleted successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dynamic tab {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting dynamic tab"));
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("nameid")?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found in token");
    }


    private static DynamicTabDto ConvertToFrontendDto(LetterTypeDefinitionDto letterType)
    {
        return new DynamicTabDto
        {
            Id = letterType.Id.ToString(),
            Name = letterType.TypeKey,
            DisplayName = letterType.DisplayName,
            Description = letterType.Description ?? string.Empty,
            IsActive = letterType.IsActive,
            CreatedAt = letterType.CreatedAt,
            DataSource = letterType.DataSourceType.ToLower(),
            HasData = false, // This would need to be calculated
            RecordCount = 0, // This would need to be calculated
            Fields = letterType.Fields.Select(f => new DynamicTabFieldDto
            {
                Id = f.Id.ToString(),
                Name = f.FieldKey,
                DisplayName = f.DisplayName,
                Type = f.FieldType.ToLower(),
                Required = f.IsRequired,
                Placeholder = f.DefaultValue ?? string.Empty,
                Order = f.OrderIndex,
                Validation = !string.IsNullOrEmpty(f.ValidationRules) ? JsonSerializer.Deserialize<object>(f.ValidationRules) : null
            }).ToList()
        };
    }

    // Letter Generation Endpoints

    [HttpPost("{tabId}/generate-letters")]
    public async Task<ActionResult<ApiResponse<object>>> GenerateLetters(string tabId, [FromBody] GenerateLettersRequest request)
    {
        try
        {
            _logger.LogInformation("Starting letter generation for tab: {TabId}, employee count: {Count}", tabId, request.EmployeeIds.Count);

            // Get the tab
            var tab = await _tabService.GetLetterTypeAsync(Guid.Parse(tabId));
            if (tab == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "Tab not found" });
            }

            // Get the template
            var template = await GetTemplateByIdAsync(request.TemplateId);
            if (template == null)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Template not found" });
            }

            // Convert LetterTypeDefinitionDto to DynamicTabDto
            var dynamicTab = ConvertToDynamicTabDto(tab);

            // Get employees from the dynamic table
            var employees = await GetEmployeesFromDynamicTable(dynamicTab, request.EmployeeIds);

            if (employees.Count == 0)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "No employees found" });
            }

            // Generate letters
            var letterBytes = await _letterGenerationService.GenerateLetterZipAsync(dynamicTab, 
                employees.Select(e => (e, template)).ToList(), 
                request.SignaturePath);

            var fileName = $"{tab.DisplayName.Replace(" ", "_")}_Letters_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

            return File(letterBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating letters for tab: {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error generating letters" });
        }
    }

    [HttpPost("{tabId}/generate-preview")]
    public async Task<ActionResult<ApiResponse<object>>> GeneratePreview(string tabId, [FromBody] GeneratePreviewRequest request)
    {
        try
        {
            _logger.LogInformation("Generating preview for tab: {TabId}, employee: {EmployeeId}", tabId, request.EmployeeId);

            // Get the tab
            var tab = await _tabService.GetLetterTypeAsync(Guid.Parse(tabId));
            if (tab == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "Tab not found" });
            }

            // Get the template
            var template = await GetTemplateByIdAsync(request.TemplateId);
            if (template == null)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Template not found" });
            }

            // Convert LetterTypeDefinitionDto to DynamicTabDto
            var dynamicTab = ConvertToDynamicTabDto(tab);

            // Get employee from the dynamic table or use provided data
            var employee = await GetEmployeeFromDynamicTable(dynamicTab, request.EmployeeId);
            if (employee == null)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Employee not found" });
            }

            // Log the employee data being passed
            _logger.LogInformation("Employee data being passed to generation service: {EmployeeData}", 
                request.EmployeeData != null ? string.Join(", ", request.EmployeeData.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "null");

            // Generate PDF preview
            var pdfBytes = await _letterGenerationService.GeneratePdfPreviewAsync(dynamicTab, employee, template, request.SignaturePath, request.EmployeeData);
            if (pdfBytes == null)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Failed to generate preview" });
            }

            var fileName = $"{employee.EmployeeId}_{tab.DisplayName.Replace(" ", "_")}_Preview.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for tab: {TabId}, employee: {EmployeeId}", tabId, request.EmployeeId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error generating preview" });
        }
    }

    private async Task<DocumentTemplate?> GetTemplateByIdAsync(string templateId)
    {
        return await _templateService.GetTemplateByIdAsync(templateId);
    }

    private DynamicTabDto ConvertToDynamicTabDto(LetterTypeDefinitionDto letterType)
    {
        return new DynamicTabDto
        {
            Id = letterType.Id.ToString(),
            Name = letterType.TypeKey,
            DisplayName = letterType.DisplayName,
            Description = letterType.Description ?? string.Empty,
            Fields = letterType.Fields.Select(f => new DynamicTabFieldDto
            {
                Id = f.Id.ToString(),
                Name = f.FieldKey,
                DisplayName = f.DisplayName,
                Type = f.FieldType,
                Required = f.IsRequired,
                Placeholder = f.DefaultValue ?? string.Empty,
                Validation = !string.IsNullOrEmpty(f.ValidationRules) ? JsonSerializer.Deserialize<object>(f.ValidationRules) : null,
                Order = f.OrderIndex
            }).ToList(),
            IsActive = letterType.IsActive,
            CreatedAt = letterType.CreatedAt,
            DataSource = letterType.DataSourceType,
            HasData = false, // This would need to be determined by checking the dynamic table
            RecordCount = 0 // This would need to be determined by checking the dynamic table
        };
    }

    private async Task<List<EmployeeDto>> GetEmployeesFromDynamicTable(DynamicTabDto tab, List<string> employeeIds)
    {
        // This would query the dynamic table to get employee data
        // For now, we'll return mock data based on the actual employee IDs
        var employees = new List<EmployeeDto>();
        
        foreach (var empId in employeeIds)
        {
            employees.Add(new EmployeeDto
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = empId,
                Name = $"Employee {empId}",
                FirstName = $"Employee",
                LastName = empId,
                Email = $"employee{empId}@example.com"
            });
        }
        
        await Task.CompletedTask; // Add await to fix warning
        return employees;
    }

    private async Task<EmployeeDto?> GetEmployeeFromDynamicTable(DynamicTabDto tab, string employeeId)
    {
        try
        {
            // Get the table schema for this tab
            var tableSchema = await _tableSchemaRepository.FirstOrDefaultAsync(ts => ts.LetterTypeDefinitionId == Guid.Parse(tab.Id));
            if (tableSchema == null)
            {
                _logger.LogWarning("No table schema found for tab: {TabId}", tab.Id);
                return null;
            }

            // Query the dynamic table to get employee data
            var tableName = tableSchema.TableName;
            var query = $"SELECT * FROM [{tableName}] WHERE [EMP ID] = @EmployeeId";
            
            using var connection = ((DbContext)_dbContext).Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = query;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@EmployeeId";
            parameter.Value = employeeId;
            command.Parameters.Add(parameter);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var employeeData = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    employeeData[columnName] = value ?? string.Empty;
                }

                // Extract employee information
                var name = GetStringValue(employeeData, "EMP NAME") ?? string.Empty;
                var email = GetStringValue(employeeData, "EMAIL") ?? string.Empty;
                var client = GetStringValue(employeeData, "CLIENT") ?? string.Empty;
                var designation = GetStringValue(employeeData, "DESIGNATION") ?? string.Empty;
                var doj = GetStringValue(employeeData, "DOJ") ?? string.Empty;
                var lwd = GetStringValue(employeeData, "LWD") ?? string.Empty;
                var ctc = GetStringValue(employeeData, "CTC") ?? string.Empty;

                return new EmployeeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployeeId = employeeId,
                    Name = name,
                    FirstName = name.Split(' ')[0],
                    LastName = name.Split(' ').Length > 1 ? string.Join(" ", name.Split(' ').Skip(1)) : string.Empty,
                    Email = email,
                    Department = client, // Map client to department
                    Position = designation // Map designation to position
                };
            }

            _logger.LogWarning("Employee {EmployeeId} not found in dynamic table {TableName}", employeeId, tableName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId} from dynamic table", employeeId);
            return null;
        }
    }

    private string? GetStringValue(Dictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value != null)
        {
            return value.ToString();
        }
        return null;
    }
}

public class GenerateLettersRequest
{
    public List<string> EmployeeIds { get; set; } = new();
    public string TemplateId { get; set; } = string.Empty;
    public string? SignaturePath { get; set; }
}

public class GeneratePreviewRequest
{
    public string EmployeeId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string? SignaturePath { get; set; }
    public Dictionary<string, object>? EmployeeData { get; set; }
}

// Frontend DTO for handling the different request format
public class FrontendCreateLetterTypeRequest
{
    [JsonPropertyName("typeKey")]
    public string TypeKey { get; set; } = string.Empty;
    
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("dataSourceType")]
    public string DataSourceType { get; set; } = string.Empty;
    
    [JsonPropertyName("fieldConfiguration")]
    public string? FieldConfiguration { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}