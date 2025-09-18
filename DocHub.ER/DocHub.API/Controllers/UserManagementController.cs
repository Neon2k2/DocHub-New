using Microsoft.AspNetCore.Mvc;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        IUserManagementService userManagementService,
        IRoleManagementService roleManagementService,
        ILogger<UserManagementController> logger)
    {
        _userManagementService = userManagementService;
        _roleManagementService = roleManagementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers([FromQuery] GetUsersRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("🔍 [USER-MGMT] Getting users for current user ID: {UserId}", currentUserId);
            
            var currentUser = await _userManagementService.GetUserByIdAsync(Guid.Parse(currentUserId));
            
            if (currentUser == null)
            {
                _logger.LogWarning("❌ [USER-MGMT] Current user not found in database: {UserId}", currentUserId);
                return Unauthorized(ApiResponse<PaginatedResponse<UserDto>>.ErrorResult("User not found"));
            }

            _logger.LogInformation("✅ [USER-MGMT] Current user found: {Username}, IsAdmin: {IsAdmin}", 
                currentUser.Username, currentUser.Permissions?.IsAdmin);

            // Non-admin users can only see users from their department
            if (currentUser.Permissions?.IsAdmin != true)
            {
                request.Department = currentUser.Department;
                _logger.LogInformation("🔒 [USER-MGMT] Non-admin user, restricting to department: {Department}", request.Department);
            }

            var result = await _userManagementService.GetUsersAsync(request);
            _logger.LogInformation("📊 [USER-MGMT] Retrieved {Count} users", result.Items?.Count() ?? 0);
            return Ok(ApiResponse<PaginatedResponse<UserDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [USER-MGMT] Error getting users");
            return StatusCode(500, ApiResponse<PaginatedResponse<UserDto>>.ErrorResult("An error occurred while getting users"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userManagementService.GetUserByIdAsync(Guid.Parse(currentUserId));
            
            if (currentUser == null)
            {
                return Unauthorized(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            // Non-admin users can only see users from their department
            if (!currentUser.Permissions.IsAdmin && user.Department != currentUser.Department)
            {
                return Forbid();
            }

            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred while getting user"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid request data"));
            }

            var created = await _userManagementService.CreateUserAsync(request, GetCurrentUserId());
            return CreatedAtAction(nameof(GetUser), new { id = created.Id }, ApiResponse<UserDto>.SuccessResult(created));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating user: {Message}", ex.Message);
            return BadRequest(ApiResponse<UserDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred while creating user"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid request data"));
            }

            var updated = await _userManagementService.UpdateUserAsync(id, request, GetCurrentUserId());
            return Ok(ApiResponse<UserDto>.SuccessResult(updated));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating user {UserId}: {Message}", id, ex.Message);
            return BadRequest(ApiResponse<UserDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred while updating user"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteUser(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (id == Guid.Parse(currentUserId))
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Cannot delete your own account"));
            }

            var deleted = await _userManagementService.DeleteUserAsync(id, GetCurrentUserId());
            if (deleted)
            {
                return Ok(ApiResponse<string>.SuccessResult("User deleted successfully"));
            }

            return BadRequest(ApiResponse<string>.ErrorResult("Failed to delete user"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while deleting user"));
        }
    }

    [HttpPost("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> ToggleUserStatus(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (id == Guid.Parse(currentUserId))
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Cannot change your own account status"));
            }

            var toggled = await _userManagementService.ToggleUserStatusAsync(id, true, GetCurrentUserId());
            if (toggled)
            {
                return Ok(ApiResponse<string>.SuccessResult("User status updated"));
            }

            return BadRequest(ApiResponse<string>.ErrorResult("Failed to update user status"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status {UserId}", id);
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while toggling user status"));
        }
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> ResetUserPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Invalid request data"));
            }

            // Not part of IUserManagementService; leaving endpoint but returning 501
            return StatusCode(501, ApiResponse<string>.ErrorResult("Reset password via admin is not implemented"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while resetting password"));
        }
    }

    [HttpGet("departments")]
    public ActionResult<ApiResponse<List<string>>> GetDepartments()
    {
        var departments = new List<string> { "ER", "Billing" };
        return Ok(ApiResponse<List<string>>.SuccessResult(departments));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<RoleDto>>>> GetRoles([FromQuery] GetRolesRequest? request = null)
    {
        try
        {
            // Ensure we have default values if request is null
            request ??= new GetRolesRequest();
            
            _logger.LogInformation("🔍 [ROLE-MGMT] Getting roles with request: Page={Page}, PageSize={PageSize}", 
                request.Page, request.PageSize);

            // Debug: Check if RoleManagementService is null
            if (_roleManagementService == null)
            {
                _logger.LogError("❌ [ROLE-MGMT] RoleManagementService is null!");
                return StatusCode(500, ApiResponse<PaginatedResponse<RoleDto>>.ErrorResult("RoleManagementService is not available"));
            }
            
            var roles = await _roleManagementService.GetRolesAsync(request);
            _logger.LogInformation("📊 [ROLE-MGMT] Retrieved {Count} roles", roles.Items?.Count() ?? 0);
            return Ok(ApiResponse<PaginatedResponse<RoleDto>>.SuccessResult(roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ROLE-MGMT] Error getting roles: {Message}", ex.Message);
            _logger.LogError(ex, "❌ [ROLE-MGMT] Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, ApiResponse<PaginatedResponse<RoleDto>>.ErrorResult($"An error occurred while getting roles: {ex.Message}"));
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found");
    }
}