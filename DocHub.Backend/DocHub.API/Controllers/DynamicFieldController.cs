using Microsoft.AspNetCore.Mvc;
using DocHub.API.DTOs;
using DocHub.API.Services.Interfaces;
using DocHub.API.Models;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DynamicFieldController : ControllerBase
{
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly ILogger<DynamicFieldController> _logger;

    public DynamicFieldController(
        IDynamicFieldService dynamicFieldService,
        ILogger<DynamicFieldController> logger)
    {
        _dynamicFieldService = dynamicFieldService;
        _logger = logger;
    }

    /// <summary>
    /// Get fields for a specific letter type definition
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DynamicField>>>> GetByLetterType([FromQuery] Guid letterTypeDefinitionId)
    {
        try
        {
            var fields = await _dynamicFieldService.GetByLetterTypeDefinitionIdAsync(letterTypeDefinitionId);
            return Ok(new ApiResponse<IEnumerable<DynamicField>>
            {
                Success = true,
                Data = fields
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dynamic fields for letter type {LetterTypeId}", letterTypeDefinitionId);
            return StatusCode(500, new ApiResponse<IEnumerable<DynamicField>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to retrieve dynamic fields"
                }
            });
        }
    }

    /// <summary>
    /// Get field by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DynamicField>>> GetById(Guid id)
    {
        try
        {
            var field = await _dynamicFieldService.GetByIdAsync(id);
            if (field == null)
            {
                return NotFound(new ApiResponse<DynamicField>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Dynamic field not found"
                    }
                });
            }

            return Ok(new ApiResponse<DynamicField>
            {
                Success = true,
                Data = field
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dynamic field {Id}", id);
            return StatusCode(500, new ApiResponse<DynamicField>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to retrieve dynamic field"
                }
            });
        }
    }

    /// <summary>
    /// Create new dynamic field
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DynamicField>>> Create([FromBody] CreateDynamicFieldRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DynamicField>
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

            var field = new DynamicField
            {
                Id = Guid.NewGuid(),
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                FieldKey = request.FieldKey,
                FieldName = request.FieldName,
                DisplayName = request.DisplayName,
                FieldType = request.FieldType,
                IsRequired = request.IsRequired,
                ValidationRules = request.ValidationRules,
                DefaultValue = request.DefaultValue,
                Order = request.Order,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdField = await _dynamicFieldService.CreateAsync(field);

            return CreatedAtAction(nameof(GetById), new { id = createdField.Id }, new ApiResponse<DynamicField>
            {
                Success = true,
                Data = createdField
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create dynamic field");
            return StatusCode(500, new ApiResponse<DynamicField>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create dynamic field"
                }
            });
        }
    }

    /// <summary>
    /// Update dynamic field
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<DynamicField>>> Update(Guid id, [FromBody] UpdateDynamicFieldRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<DynamicField>
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

            var existingField = await _dynamicFieldService.GetByIdAsync(id);
            if (existingField == null)
            {
                return NotFound(new ApiResponse<DynamicField>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Dynamic field not found"
                    }
                });
            }

            existingField.FieldKey = request.FieldKey;
            existingField.FieldName = request.FieldName;
            existingField.DisplayName = request.DisplayName;
            existingField.FieldType = request.FieldType;
            existingField.IsRequired = request.IsRequired;
            existingField.ValidationRules = request.ValidationRules;
            existingField.DefaultValue = request.DefaultValue;
            existingField.Order = request.Order;
            existingField.UpdatedAt = DateTime.UtcNow;

            var updatedField = await _dynamicFieldService.UpdateAsync(existingField);

            return Ok(new ApiResponse<DynamicField>
            {
                Success = true,
                Data = updatedField
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update dynamic field {Id}", id);
            return StatusCode(500, new ApiResponse<DynamicField>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update dynamic field"
                }
            });
        }
    }

    /// <summary>
    /// Delete dynamic field
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        try
        {
            var existingField = await _dynamicFieldService.GetByIdAsync(id);
            if (existingField == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = "Dynamic field not found"
                    }
                });
            }

            await _dynamicFieldService.DeleteAsync(id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Dynamic field deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete dynamic field {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete dynamic field"
                }
            });
        }
    }

    /// <summary>
    /// Reorder fields for a letter type
    /// </summary>
    [HttpPost("reorder")]
    public async Task<ActionResult<ApiResponse<object>>> ReorderFields([FromBody] ReorderFieldsRequest request)
    {
        try
        {
            await _dynamicFieldService.ReorderFieldsAsync(request.LetterTypeDefinitionId, request.FieldOrders);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Fields reordered successfully" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder fields for letter type {LetterTypeId}", request.LetterTypeDefinitionId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to reorder fields"
                }
            });
        }
    }

    /// <summary>
    /// Validate field configuration
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<FieldValidationResult>>> ValidateField([FromBody] ValidateFieldRequest request)
    {
        try
        {
            var result = await _dynamicFieldService.ValidateFieldAsync(request);
            
            return Ok(new ApiResponse<FieldValidationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate field");
            return StatusCode(500, new ApiResponse<FieldValidationResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to validate field"
                }
            });
        }
    }
}
