using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _moduleService;
    private readonly ILogger<ModulesController> _logger;

    public ModulesController(
        IModuleService moduleService,
        ILogger<ModulesController> logger)
    {
        _moduleService = moduleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all modules
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ModuleSummary>>>> GetModules()
    {
        try
        {
            var modules = await _moduleService.GetModulesAsync();

            return Ok(new ApiResponse<List<ModuleSummary>>
            {
                Success = true,
                Data = modules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get modules");
            return StatusCode(500, new ApiResponse<List<ModuleSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get modules"
                }
            });
        }
    }

    /// <summary>
    /// Get module by ID
    /// </summary>
    [HttpGet("{moduleId}")]
    public async Task<ActionResult<ApiResponse<ModuleDetail>>> GetModule(Guid moduleId)
    {
        try
        {
            var module = await _moduleService.GetModuleAsync(moduleId);

            return Ok(new ApiResponse<ModuleDetail>
            {
                Success = true,
                Data = module
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ModuleDetail>
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
            _logger.LogError(ex, "Failed to get module {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<ModuleDetail>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get module"
                }
            });
        }
    }

    /// <summary>
    /// Create module
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ModuleSummary>>> CreateModule([FromBody] CreateModuleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ModuleSummary>
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

            var module = await _moduleService.CreateModuleAsync(request);

            return Ok(new ApiResponse<ModuleSummary>
            {
                Success = true,
                Data = module
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create module");
            return StatusCode(500, new ApiResponse<ModuleSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create module"
                }
            });
        }
    }

    /// <summary>
    /// Update module
    /// </summary>
    [HttpPut("{moduleId}")]
    public async Task<ActionResult<ApiResponse<ModuleSummary>>> UpdateModule(
        Guid moduleId,
        [FromBody] UpdateModuleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ModuleSummary>
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

            var module = await _moduleService.UpdateModuleAsync(moduleId, request);

            return Ok(new ApiResponse<ModuleSummary>
            {
                Success = true,
                Data = module
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ModuleSummary>
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
            _logger.LogError(ex, "Failed to update module {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<ModuleSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update module"
                }
            });
        }
    }

    /// <summary>
    /// Delete module
    /// </summary>
    [HttpDelete("{moduleId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteModule(Guid moduleId)
    {
        try
        {
            await _moduleService.DeleteModuleAsync(moduleId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Module deleted successfully" }
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
            _logger.LogError(ex, "Failed to delete module {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete module"
                }
            });
        }
    }

    /// <summary>
    /// Get module statistics
    /// </summary>
    [HttpGet("{moduleId}/statistics")]
    public async Task<ActionResult<ApiResponse<ModuleStatistics>>> GetModuleStatistics(Guid moduleId)
    {
        try
        {
            var statistics = await _moduleService.GetModuleStatisticsAsync(moduleId);

            return Ok(new ApiResponse<ModuleStatistics>
            {
                Success = true,
                Data = statistics
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ModuleStatistics>
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
            _logger.LogError(ex, "Failed to get module statistics for {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<ModuleStatistics>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get module statistics"
                }
            });
        }
    }

    /// <summary>
    /// Get module letter types
    /// </summary>
    [HttpGet("{moduleId}/letter-types")]
    public async Task<ActionResult<ApiResponse<List<LetterTypeSummary>>>> GetModuleLetterTypes(Guid moduleId)
    {
        try
        {
            var letterTypes = await _moduleService.GetModuleLetterTypesAsync(moduleId);

            return Ok(new ApiResponse<List<LetterTypeSummary>>
            {
                Success = true,
                Data = letterTypes
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<List<LetterTypeSummary>>
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
            _logger.LogError(ex, "Failed to get module letter types for {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<List<LetterTypeSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get module letter types"
                }
            });
        }
    }

    /// <summary>
    /// Get module recent activities
    /// </summary>
    [HttpGet("{moduleId}/activities")]
    public async Task<ActionResult<ApiResponse<List<ActivitySummary>>>> GetModuleActivities(
        Guid moduleId,
        [FromQuery] int limit = 10)
    {
        try
        {
            var activities = await _moduleService.GetModuleActivitiesAsync(moduleId, limit);

            return Ok(new ApiResponse<List<ActivitySummary>>
            {
                Success = true,
                Data = activities
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<List<ActivitySummary>>
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
            _logger.LogError(ex, "Failed to get module activities for {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<List<ActivitySummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get module activities"
                }
            });
        }
    }

    /// <summary>
    /// Get module dashboard data
    /// </summary>
    [HttpGet("{moduleId}/dashboard")]
    public async Task<ActionResult<ApiResponse<ModuleDashboard>>> GetModuleDashboard(Guid moduleId)
    {
        try
        {
            var dashboard = await _moduleService.GetModuleDashboardAsync(moduleId);

            return Ok(new ApiResponse<ModuleDashboard>
            {
                Success = true,
                Data = dashboard
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ModuleDashboard>
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
            _logger.LogError(ex, "Failed to get module dashboard for {ModuleId}", moduleId);
            return StatusCode(500, new ApiResponse<ModuleDashboard>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get module dashboard"
                }
            });
        }
    }
}
