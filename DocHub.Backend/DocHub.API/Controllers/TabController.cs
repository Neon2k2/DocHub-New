using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Tabs;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Emails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocHub.Core.Entities;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;

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
    private readonly IEmailService _emailService;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TabController> _logger;

    public TabController(ITabManagementService tabService, IDynamicLetterGenerationService letterGenerationService, ITemplateService templateService, IRepository<TableSchema> tableSchemaRepository, IDbContext dbContext, IEmailService emailService, IDepartmentAccessService departmentAccessService, IConfiguration configuration, ILogger<TabController> logger)
    {
        _tabService = tabService;
        _letterGenerationService = letterGenerationService;
        _templateService = templateService;
        _tableSchemaRepository = tableSchemaRepository;
        _dbContext = dbContext;
        _emailService = emailService;
        _departmentAccessService = departmentAccessService;
        _configuration = configuration;
        _logger = logger;
    }

    // Letter Type Management Endpoints

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LetterTypeDefinitionDto>>>> GetLetterTypes()
    {
        try
        {
            var userId = GetCurrentUserId();
            var accessibleTabIds = await _departmentAccessService.GetAccessibleTabIds(Guid.Parse(userId));
            
            var letterTypes = await _tabService.GetLetterTypesAsync();
            var filteredLetterTypes = letterTypes.Where(lt => accessibleTabIds.Contains(lt.Id));
            
            return Ok(ApiResponse<IEnumerable<LetterTypeDefinitionDto>>.SuccessResult(filteredLetterTypes));
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
            var userId = GetCurrentUserId();
            
            // Check if user can access this tab
            if (!await _departmentAccessService.UserCanAccessTab(Guid.Parse(userId), id))
            {
                return Forbid("You don't have access to this tab");
            }

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
            
            // Get current user's department
            var currentUserId = GetCurrentUserId();
            var user = await _dbContext.Users.FindAsync(Guid.Parse(currentUserId));
            var userDepartment = user?.Department ?? "ER"; // Default to ER if not set
            
            // Create request without module requirement
            var request = new CreateLetterTypeRequest
            {
                TypeKey = frontendData.TypeKey,
                DisplayName = frontendData.DisplayName,
                Description = frontendData.Description,
                DataSourceType = frontendData.DataSourceType,
                Department = userDepartment, // Set department based on current user
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

    [HttpPut("employee-data/{tabId}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateEmployeeData(string tabId, [FromBody] UpdateEmployeeDataRequest request)
    {
        _logger.LogInformation("🚀 [UPDATE-EMPLOYEE] Method entry - tabId: {TabId}, request: {Request}", tabId, request != null ? "not null" : "null");
        _logger.LogInformation("🚀 [UPDATE-EMPLOYEE] Route matched successfully!");
        
        // Check if request is null
        if (request == null)
        {
            _logger.LogError("❌ [UPDATE-EMPLOYEE] Request is null");
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Request body is required" });
        }
        
        // Log the request details
        _logger.LogInformation("🚀 [UPDATE-EMPLOYEE] Request details - EmployeeId: {EmployeeId}, Field: {Field}, Value: {Value}", 
            request.EmployeeId, request.Field, request.Value);
        
        // Check ModelState for validation errors
        if (!ModelState.IsValid)
        {
            _logger.LogError("❌ [UPDATE-EMPLOYEE] ModelState is invalid");
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogError("❌ [UPDATE-EMPLOYEE] Validation errors: {Errors}", string.Join(", ", errors));
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });
        }
        
        try
        {
            // Parse the tabId as GUID
            if (!Guid.TryParse(tabId, out var tabIdGuid))
            {
                _logger.LogError("❌ [UPDATE-EMPLOYEE] Invalid tabId format: {TabId}", tabId);
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid tab ID format" });
            }
            
            _logger.LogInformation("Updating employee data for tab: {TabId}, employee: {EmployeeId}, field: {Field}", 
                tabId, request.EmployeeId, request.Field);

            // Get the tab
            var tab = await _tabService.GetLetterTypeAsync(tabIdGuid);
            if (tab == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "Tab not found" });
            }

            // Get the table schema for this tab
            var tableSchema = await _tableSchemaRepository.FirstOrDefaultAsync(ts => ts.LetterTypeDefinitionId == tabIdGuid);
            if (tableSchema == null)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "No data table found for this tab" });
            }

            // Log the table schema details
            _logger.LogInformation("Table schema found - TableName: {TableName}, ColumnDefinitions: {ColumnDefinitions}", 
                tableSchema.TableName, tableSchema.ColumnDefinitions);

            // Update the employee data in the dynamic table
            var tableName = tableSchema.TableName;
            _logger.LogInformation("Using table name: {TableName} for update", tableName);
            
            // Validate that the field exists in the table schema
            // For now, we'll skip the column validation since the table schema might not have the exact column names
            // The field validation will be done at the database level when we execute the query
            _logger.LogInformation("Skipping column validation - will validate at database level");
            
            var query = $"UPDATE [{tableName}] SET [{request.Field}] = @Value WHERE [EMP ID] = @EmployeeId";
            _logger.LogInformation("Executing query: {Query}", query);
            
            using var connection = ((DbContext)_dbContext).Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var valueParameter = command.CreateParameter();
            valueParameter.ParameterName = "@Value";
            valueParameter.Value = request.Value ?? string.Empty;
            command.Parameters.Add(valueParameter);
            
            var employeeIdParameter = command.CreateParameter();
            employeeIdParameter.ParameterName = "@EmployeeId";
            employeeIdParameter.Value = request.EmployeeId;
            command.Parameters.Add(employeeIdParameter);

            _logger.LogInformation("Executing command with parameters - Value: {Value}, EmployeeId: {EmployeeId}", 
                request.Value, request.EmployeeId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Query executed, rows affected: {RowsAffected}", rowsAffected);
            
            if (rowsAffected == 0)
            {
                _logger.LogWarning("No rows were updated for EmployeeId: {EmployeeId} in table: {TableName}", 
                    request.EmployeeId, tableName);
                return NotFound(new ApiResponse<object> { Success = false, Message = "Employee not found or field does not exist" });
            }

            _logger.LogInformation("Successfully updated employee data for tab: {TabId}, employee: {EmployeeId}, field: {Field}", 
                tabId, request.EmployeeId, request.Field);

            return Ok(new ApiResponse<object> { Success = true, Message = "Employee data updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee data for tab: {TabId}, employee: {EmployeeId}, field: {Field}, value: {Value}", 
                tabId, request.EmployeeId, request.Field, request.Value);
            _logger.LogError("Exception details: {ExceptionMessage}", ex.Message);
            
            // Check if it's a SQL exception about invalid column name
            if (ex.Message.Contains("Invalid column name") || ex.Message.Contains("Column") && ex.Message.Contains("does not exist"))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = $"Field '{request.Field}' does not exist in the table" });
            }
            
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = $"Error updating employee data: {ex.Message}" });
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

    [HttpGet("{tabId}/email-history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailJobDto>>>> GetEmailHistory(string tabId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Getting email history for tab: {TabId}, page: {Page}, pageSize: {PageSize}", tabId, page, pageSize);

            // Try to get from database first
            try
            {
                var emailJobs = await _dbContext.EmailJobs
                    .Where(e => e.LetterTypeDefinitionId == Guid.Parse(tabId))
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var emailJobDtos = emailJobs.Select(e => new EmailJobDto
                {
                    Id = e.Id,
                    LetterTypeDefinitionId = e.LetterTypeDefinitionId,
                    LetterTypeName = "Dynamic Tab", // You might want to get this from the tab
                    ExcelUploadId = e.ExcelUploadId,
                    Subject = e.Subject,
                    Content = e.Content,
                    Attachments = e.Attachments,
                    Status = e.Status,
                    SentBy = e.SentBy,
                    EmployeeId = e.RecipientEmail, // Using email as ID for now
                    EmployeeName = e.RecipientName,
                    EmployeeEmail = e.RecipientEmail,
                    RecipientEmail = e.RecipientEmail,
                    RecipientName = e.RecipientName,
                    CreatedAt = e.CreatedAt,
                    SentAt = e.SentAt,
                    DeliveredAt = e.DeliveredAt,
                    OpenedAt = e.OpenedAt,
                    ClickedAt = e.ClickedAt,
                    BouncedAt = e.BouncedAt,
                    DroppedAt = e.DroppedAt,
                    SpamReportedAt = e.SpamReportedAt,
                    UnsubscribedAt = e.UnsubscribedAt,
                    BounceReason = e.BounceReason,
                    DropReason = e.DropReason,
                    TrackingId = e.TrackingId,
                    SendGridMessageId = e.SendGridMessageId,
                    ErrorMessage = e.ErrorMessage,
                    ProcessedAt = e.ProcessedAt,
                    RetryCount = e.RetryCount,
                    LastRetryAt = e.LastRetryAt,
                    UpdatedAt = e.UpdatedAt
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<EmailJobDto>> { Success = true, Data = emailJobDtos });
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Database query failed, trying file system fallback");
                
                // Fallback: Get from file system
                var historyDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "email-history");
                if (Directory.Exists(historyDir))
                {
                    var files = Directory.GetFiles(historyDir, "email_*.json")
                        .OrderByDescending(f => System.IO.File.GetCreationTime(f))
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize);

                    var emailJobDtos = new List<EmailJobDto>();
                    foreach (var file in files)
                    {
                        try
                        {
                            var json = await System.IO.File.ReadAllTextAsync(file);
                            var historyData = JsonSerializer.Deserialize<EmailJobDto>(json);
                            if (historyData != null)
                            {
                                emailJobDtos.Add(historyData);
                            }
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogWarning(fileEx, "Failed to read email history file: {File}", file);
                        }
                    }

                    return Ok(new ApiResponse<IEnumerable<EmailJobDto>> { Success = true, Data = emailJobDtos });
                }

                return Ok(new ApiResponse<IEnumerable<EmailJobDto>> { Success = true, Data = new List<EmailJobDto>() });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email history for tab: {TabId}", tabId);
            return StatusCode(500, new ApiResponse<IEnumerable<EmailJobDto>> { Success = false, Message = "Error getting email history" });
        }
    }

    [HttpGet("email-status")]
    public ActionResult<ApiResponse<object>> GetEmailStatus()
    {
        try
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var skipSending = _configuration["SendGrid:SkipSending"] == "true";
            
            var status = new
            {
                SendGridConfigured = !string.IsNullOrEmpty(apiKey),
                SendingEnabled = !skipSending,
                FromEmail = _configuration["SendGrid:FromEmail"],
                FromName = _configuration["SendGrid:FromName"],
                Status = skipSending ? "Disabled (Testing Mode)" : "Enabled",
                Message = skipSending ? "Email sending is disabled for testing. Change 'SkipSending' to 'false' in appsettings.json to enable." : "Email sending is enabled."
            };

            return Ok(new ApiResponse<object> { Success = true, Data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email status");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error getting email status" });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("nameid")?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found in token");
    }

    private async Task SendEmailDirectlyAsync(string subject, string content, string toEmail, string toName, string attachmentsJson)
    {
        try
        {
            // Get SendGrid configuration
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("SendGrid API key is not configured");
            }

            // Check if we should skip sending due to credits (for testing)
            var skipSending = _configuration["SendGrid:SkipSending"] == "true";
            if (skipSending)
            {
                _logger.LogWarning("SendGrid sending is disabled via configuration. Email would have been sent to {Email}", toEmail);
                throw new InvalidOperationException("SendGrid sending is disabled for testing");
            }

            // Create SendGrid client
            var client = new SendGrid.SendGridClient(apiKey);

            // Parse attachments if any
            var attachments = new List<SendGrid.Helpers.Mail.Attachment>();
            if (!string.IsNullOrEmpty(attachmentsJson))
            {
                try
                {
                    var attachmentList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(attachmentsJson);
                    if (attachmentList != null)
                    {
                        foreach (var att in attachmentList)
                        {
                            if (att.TryGetValue("fileName", out var fileName) &&
                                att.TryGetValue("mimeType", out var mimeType) &&
                                att.TryGetValue("content", out var contentBase64))
                            {
                                var attachment = new SendGrid.Helpers.Mail.Attachment
                                {
                                    Content = contentBase64.ToString(),
                                    Type = mimeType.ToString(),
                                    Filename = fileName.ToString()
                                };
                                attachments.Add(attachment);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse attachments, sending email without attachments");
                }
            }

            // Create email message
            var message = new SendGrid.Helpers.Mail.SendGridMessage()
            {
                From = new SendGrid.Helpers.Mail.EmailAddress(fromEmail, fromName),
                Subject = subject,
                PlainTextContent = StripHtml(content),
                HtmlContent = content
            };

            message.AddTo(new SendGrid.Helpers.Mail.EmailAddress(toEmail, toName));

            // Add attachments
            if (attachments.Count > 0)
            {
                message.Attachments = attachments;
            }

            // Send email
            var response = await client.SendEmailAsync(message);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email} with status {StatusCode}", toEmail, response.StatusCode);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                throw new HttpRequestException($"SendGrid API returned status code: {response.StatusCode}. Response: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email directly to {Email}", toEmail);
            throw;
        }
    }

    private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
    }

    private async Task SaveEmailHistoryAsync(EmailJob emailJob, string status, string? errorMessage)
    {
        try
        {
            // Update email job status
            emailJob.Status = status;
            emailJob.UpdatedAt = DateTime.UtcNow;
            
            if (status == "sent")
            {
                emailJob.SentAt = DateTime.UtcNow;
            }
            else if (status == "failed" && !string.IsNullOrEmpty(errorMessage))
            {
                emailJob.ErrorMessage = errorMessage;
            }
            else if (status == "queued" && !string.IsNullOrEmpty(errorMessage))
            {
                emailJob.ErrorMessage = errorMessage;
            }

            // Always save to file system first (more reliable)
            await SaveEmailHistoryToFileAsync(emailJob);
            
            // Try to save to database as well (if possible)
            try
            {
                // Create a new context scope to avoid disposal issues
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
                
                await dbContext.EmailJobs.AddAsync(emailJob);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Email history saved to database for job {EmailJobId}", emailJob.Id);
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Failed to save email history to database for job {EmailJobId}. Using file system only.", emailJob.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving email history for job {EmailJobId}", emailJob.Id);
        }
    }

    private async Task SaveEmailHistoryToFileAsync(EmailJob emailJob)
    {
        try
        {
            var historyDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "email-history");
            Directory.CreateDirectory(historyDir);
            
            var fileName = $"email_{emailJob.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(historyDir, fileName);
            
            var historyData = new
            {
                Id = emailJob.Id,
                LetterTypeDefinitionId = emailJob.LetterTypeDefinitionId,
                Subject = emailJob.Subject,
                Content = emailJob.Content,
                RecipientEmail = emailJob.RecipientEmail,
                RecipientName = emailJob.RecipientName,
                Status = emailJob.Status,
                SentBy = emailJob.SentBy,
                CreatedAt = emailJob.CreatedAt,
                SentAt = emailJob.SentAt,
                ErrorMessage = emailJob.ErrorMessage,
                Attachments = emailJob.Attachments
            };
            
            var json = JsonSerializer.Serialize(historyData, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
            
            _logger.LogInformation("Email history saved to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving email history to file for job {EmailJobId}", emailJob.Id);
        }
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



    [HttpPost("{tabId}/send-email")]
    public async Task<ActionResult<ApiResponse<EmailJobDto>>> SendEmailWithPdf(string tabId, [FromBody] SendEmailWithPdfRequest request)
    {
        try
        {
            _logger.LogInformation("Sending email with PDF for tab: {TabId}, employee: {EmployeeId}", tabId, request.EmployeeId);

            // Get the tab
            var tab = await _tabService.GetLetterTypeAsync(Guid.Parse(tabId));
            if (tab == null)
            {
                return NotFound(new ApiResponse<EmailJobDto> { Success = false, Message = "Tab not found" });
            }

            // Get the template
            var template = await GetTemplateByIdAsync(request.TemplateId);
            if (template == null)
            {
                return BadRequest(new ApiResponse<EmailJobDto> { Success = false, Message = "Template not found" });
            }

            // Convert LetterTypeDefinitionDto to DynamicTabDto
            var dynamicTab = ConvertToDynamicTabDto(tab);

            // Get employee from the dynamic table
            var employee = await GetEmployeeFromDynamicTable(dynamicTab, request.EmployeeId);
            if (employee == null)
            {
                return BadRequest(new ApiResponse<EmailJobDto> { Success = false, Message = "Employee not found" });
            }

            if (string.IsNullOrEmpty(employee.Email))
            {
                return BadRequest(new ApiResponse<EmailJobDto> { Success = false, Message = "Employee email not found" });
            }

            // Generate PDF
            var pdfBytes = await _letterGenerationService.GeneratePdfPreviewAsync(dynamicTab, employee, template, request.SignaturePath, request.EmployeeData);
            if (pdfBytes == null)
            {
                return BadRequest(new ApiResponse<EmailJobDto> { Success = false, Message = "Failed to generate PDF" });
            }

            // Create attachments for SendGrid
            var attachments = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["fileName"] = $"{employee.Name}_Document.pdf",
                    ["mimeType"] = "application/pdf",
                    ["content"] = Convert.ToBase64String(pdfBytes)
                }
            };

            // Add any extra attachments
            if (request.ExtraAttachments != null && request.ExtraAttachments.Any())
            {
                foreach (var attachment in request.ExtraAttachments)
                {
                    attachments.Add(new Dictionary<string, object>
                    {
                        ["fileName"] = attachment.FileName,
                        ["mimeType"] = attachment.MimeType,
                        ["content"] = attachment.Content
                    });
                }
            }

            var attachmentsJson = JsonSerializer.Serialize(attachments);

            // Create email job for response (without database save for now)
            var emailJob = new EmailJob
            {
                Id = Guid.NewGuid(),
                LetterTypeDefinitionId = Guid.Parse(tabId),
                ExcelUploadId = Guid.Empty, // Not used for dynamic tabs
                Subject = request.Subject,
                Content = request.Content,
                RecipientEmail = employee.Email,
                RecipientName = employee.Name,
                Status = "pending",
                SentBy = Guid.Parse(GetCurrentUserId()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Attachments = attachmentsJson
            };

            // Send email via SendGrid directly (bypassing EmailService database dependency)
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendEmailDirectlyAsync(request.Subject, request.Content, employee.Email, employee.Name, attachmentsJson);
                    
                    // Try to save email history (with error handling)
                    await SaveEmailHistoryAsync(emailJob, "sent", null);
                    
                    _logger.LogInformation("Email sent successfully to {Email}", employee.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email for job {EmailJobId} to {Email}", emailJob.Id, employee.Email);
                    
                    // Check if it's a SendGrid credits issue
                    if (ex.Message.Contains("Maximum credits exceeded"))
                    {
                        _logger.LogWarning("SendGrid credits exceeded. Email queued for later sending.");
                        await SaveEmailHistoryAsync(emailJob, "queued", "SendGrid credits exceeded - will retry later");
                    }
                    else
                    {
                        // Try to save failed status
                        await SaveEmailHistoryAsync(emailJob, "failed", ex.Message);
                    }
                }
            });

            // Map to DTO
            var emailJobDto = new EmailJobDto
            {
                Id = emailJob.Id,
                LetterTypeDefinitionId = emailJob.LetterTypeDefinitionId,
                LetterTypeName = tab.DisplayName,
                ExcelUploadId = emailJob.ExcelUploadId,
                Subject = emailJob.Subject,
                Content = emailJob.Content,
                Attachments = attachmentsJson,
                Status = emailJob.Status,
                SentBy = emailJob.SentBy,
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.Name,
                EmployeeEmail = employee.Email,
                RecipientEmail = emailJob.RecipientEmail,
                RecipientName = emailJob.RecipientName,
                CreatedAt = emailJob.CreatedAt,
                SentAt = emailJob.SentAt,
                DeliveredAt = emailJob.DeliveredAt,
                OpenedAt = emailJob.OpenedAt,
                ClickedAt = emailJob.ClickedAt,
                BouncedAt = emailJob.BouncedAt,
                DroppedAt = emailJob.DroppedAt,
                SpamReportedAt = emailJob.SpamReportedAt,
                UnsubscribedAt = emailJob.UnsubscribedAt,
                BounceReason = emailJob.BounceReason,
                DropReason = emailJob.DropReason,
                TrackingId = emailJob.TrackingId,
                SendGridMessageId = emailJob.SendGridMessageId,
                ErrorMessage = emailJob.ErrorMessage,
                ProcessedAt = emailJob.ProcessedAt,
                RetryCount = emailJob.RetryCount,
                LastRetryAt = emailJob.LastRetryAt,
                UpdatedAt = emailJob.UpdatedAt
            };

            return Ok(new ApiResponse<EmailJobDto> { Success = true, Data = emailJobDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email with PDF for tab: {TabId}, employee: {EmployeeId}", tabId, request.EmployeeId);
            return StatusCode(500, new ApiResponse<EmailJobDto> { Success = false, Message = "Error sending email" });
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

public class UpdateEmployeeDataRequest
{
    [JsonPropertyName("EmployeeId")]
    public string EmployeeId { get; set; } = string.Empty;
    
    [JsonPropertyName("Field")]
    public string Field { get; set; } = string.Empty;
    
    [JsonPropertyName("Value")]
    public string? Value { get; set; }
}

public class SendEmailWithPdfRequest
{
    [JsonPropertyName("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;
    
    [JsonPropertyName("templateId")]
    public string TemplateId { get; set; } = string.Empty;
    
    [JsonPropertyName("signaturePath")]
    public string? SignaturePath { get; set; }
    
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("employeeData")]
    public Dictionary<string, object>? EmployeeData { get; set; }
    
    [JsonPropertyName("extraAttachments")]
    public List<EmailAttachmentRequest>? ExtraAttachments { get; set; }
}

public class EmailAttachmentRequest
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}