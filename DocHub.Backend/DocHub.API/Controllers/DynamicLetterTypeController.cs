using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DocHub.API.DTOs;
using DocHub.API.Services.Interfaces;
using DocHub.API.Models;
using DocHub.API.Attributes;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DynamicLetterTypeController : ControllerBase
{
    private readonly ILetterTypeService _letterTypeService;
    private readonly ILogger<DynamicLetterTypeController> _logger;

    public DynamicLetterTypeController(
        ILetterTypeService letterTypeService,
        ILogger<DynamicLetterTypeController> logger)
    {
        _letterTypeService = letterTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all letter type definitions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LetterTypeDefinition>>>> GetAll()
    {
        try
        {
            var letterTypes = await _letterTypeService.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<LetterTypeDefinition>>
            {
                Success = true,
                Data = letterTypes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get letter type definitions");
            return StatusCode(500, new ApiResponse<IEnumerable<LetterTypeDefinition>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to retrieve letter type definitions"
                }
            });
        }
    }

    /// <summary>
    /// Get letter type definition by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinition>>> GetById(Guid id)
    {
        try
        {
            var letterType = await _letterTypeService.GetByIdAsync(id);
            if (letterType == null)
            {
                return NotFound(new ApiResponse<LetterTypeDefinition>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Letter type definition not found"
                    }
                });
            }

            return Ok(new ApiResponse<LetterTypeDefinition>
            {
                Success = true,
                Data = letterType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get letter type definition {Id}", id);
            return StatusCode(500, new ApiResponse<LetterTypeDefinition>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to retrieve letter type definition"
                }
            });
        }
    }

    /// <summary>
    /// Create new letter type definition
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinition>>> Create([FromBody] CreateLetterTypeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<LetterTypeDefinition>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request data",
                        Details = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var letterType = new LetterTypeDefinition
            {
                Id = Guid.NewGuid(),
                TypeKey = request.TypeKey,
                DisplayName = request.DisplayName,
                Description = request.Description,
                FieldConfiguration = request.FieldConfiguration,
                IsActive = request.IsActive,
                ModuleId = Guid.Parse(request.Module),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdLetterType = await _letterTypeService.CreateAsync(letterType);

            return CreatedAtAction(nameof(GetById), new { id = createdLetterType.Id }, new ApiResponse<LetterTypeDefinition>
            {
                Success = true,
                Data = createdLetterType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create letter type definition");
            return StatusCode(500, new ApiResponse<LetterTypeDefinition>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create letter type definition"
                }
            });
        }
    }

    /// <summary>
    /// Update letter type definition
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinition>>> Update(Guid id, [FromBody] UpdateLetterTypeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<LetterTypeDefinition>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request data",
                        Details = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var existingLetterType = await _letterTypeService.GetByIdAsync(id);
            if (existingLetterType == null)
            {
                return NotFound(new ApiResponse<LetterTypeDefinition>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Letter type definition not found"
                    }
                });
            }

            existingLetterType.DisplayName = request.DisplayName;
            existingLetterType.Description = request.Description;
            existingLetterType.FieldConfiguration = request.FieldConfiguration;
            existingLetterType.IsActive = request.IsActive;
            existingLetterType.UpdatedAt = DateTime.UtcNow;

            var updatedLetterType = await _letterTypeService.UpdateAsync(id, existingLetterType);

            return Ok(new ApiResponse<LetterTypeDefinition>
            {
                Success = true,
                Data = updatedLetterType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update letter type definition {Id}", id);
            return StatusCode(500, new ApiResponse<LetterTypeDefinition>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update letter type definition"
                }
            });
        }
    }

    /// <summary>
    /// Delete letter type definition
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        try
        {
            var existingLetterType = await _letterTypeService.GetByIdAsync(id);
            if (existingLetterType == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Letter type definition not found"
                    }
                });
            }

            await _letterTypeService.DeleteAsync(id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Letter type definition deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete letter type definition {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete letter type definition"
                }
            });
        }
    }

    /// <summary>
    /// Get letter types by module
    /// </summary>
    [HttpGet("module/{module}")]
    [ModuleAccess("ER")] // This will be dynamic based on the module parameter
    public async Task<ActionResult<ApiResponse<IEnumerable<LetterTypeDefinition>>>> GetByModule(string module)
    {
        try
        {
            var letterTypes = await _letterTypeService.GetByModuleAsync(module);
            return Ok(new ApiResponse<IEnumerable<LetterTypeDefinition>>
            {
                Success = true,
                Data = letterTypes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get letter types for module {Module}", module);
            return StatusCode(500, new ApiResponse<IEnumerable<LetterTypeDefinition>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to retrieve letter types for module"
                }
            });
        }
    }

    /// <summary>
    /// Activate/Deactivate letter type
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult<ApiResponse<LetterTypeDefinition>>> ToggleStatus(Guid id)
    {
        try
        {
            var letterType = await _letterTypeService.GetByIdAsync(id);
            if (letterType == null)
            {
                return NotFound(new ApiResponse<LetterTypeDefinition>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Letter type definition not found"
                    }
                });
            }

            letterType.IsActive = !letterType.IsActive;
            letterType.UpdatedAt = DateTime.UtcNow;

            var updatedLetterType = await _letterTypeService.UpdateAsync(id, letterType);

            return Ok(new ApiResponse<LetterTypeDefinition>
            {
                Success = true,
                Data = updatedLetterType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle status for letter type {Id}", id);
            return StatusCode(500, new ApiResponse<LetterTypeDefinition>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to toggle letter type status"
                }
            });
        }
    }
}