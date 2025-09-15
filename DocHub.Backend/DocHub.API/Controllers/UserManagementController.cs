using DocHub.Core.Interfaces;
using DocHub.Core.Entities;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(IUserManagementService userManagementService, IPasswordPolicyService passwordPolicyService, ILogger<UserManagementController> logger)
    {
        _userManagementService = userManagementService;
        _passwordPolicyService = passwordPolicyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> GetUsers([FromQuery] GetUsersRequest request)
    {
        try
        {
            var users = await _userManagementService.GetUsersAsync(request);
            return Ok(ApiResponse<PaginatedResponse<UserDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, ApiResponse<PaginatedResponse<UserDto>>.ErrorResult("An error occurred while getting users"));
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid userId)
    {
        try
        {
            var user = await _userManagementService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user: {UserId}", userId);
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

            var currentUserId = GetCurrentUserId();
            var user = await _userManagementService.CreateUserAsync(request, currentUserId);
            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred while creating user"));
        }
    }

    [HttpPut("{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var user = await _userManagementService.UpdateUserAsync(userId, request, currentUserId);
            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", userId);
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred while updating user"));
        }
    }

    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _userManagementService.DeleteUserAsync(userId, currentUserId);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("User not found"));
            }

            return Ok(ApiResponse<bool>.SuccessResult(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while deleting user"));
        }
    }

    [HttpPatch("{userId}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleUserStatus(Guid userId, [FromBody] bool isActive)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _userManagementService.ToggleUserStatusAsync(userId, isActive, currentUserId);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("User not found"));
            }

            return Ok(ApiResponse<bool>.SuccessResult(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status: {UserId}", userId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while toggling user status"));
        }
    }

    [HttpPost("{userId}/change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(Guid userId, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _userManagementService.ChangePasswordAsync(userId, request, currentUserId);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResult(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while changing password"));
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var result = await _userManagementService.ResetPasswordAsync(request);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while resetting password"));
        }
    }

    [HttpPost("confirm-password-reset")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmPasswordReset([FromBody] ResetPasswordConfirmRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var result = await _userManagementService.ConfirmPasswordResetAsync(request);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while confirming password reset"));
        }
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> GetUserStats()
    {
        try
        {
            var stats = await _userManagementService.GetUserStatsAsync();
            return Ok(ApiResponse<UserStatsDto>.SuccessResult(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats");
            return StatusCode(500, ApiResponse<UserStatsDto>.ErrorResult("An error occurred while getting user stats"));
        }
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetRecentUsers([FromQuery] int count = 10)
    {
        try
        {
            var users = await _userManagementService.GetRecentUsersAsync(count);
            return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent users");
            return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResult("An error occurred while getting recent users"));
        }
    }

    [HttpGet("by-role/{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByRole(string role)
    {
        try
        {
            var users = await _userManagementService.GetUsersByRoleAsync(role);
            return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role: {Role}", role);
            return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResult("An error occurred while getting users by role"));
        }
    }

    [HttpGet("by-department/{department}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByDepartment(string department)
    {
        try
        {
            var users = await _userManagementService.GetUsersByDepartmentAsync(department);
            return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by department: {Department}", department);
            return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResult("An error occurred while getting users by department"));
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> SearchUsers([FromQuery] string searchTerm, [FromQuery] int limit = 20)
    {
        try
        {
            var users = await _userManagementService.SearchUsersAsync(searchTerm, limit);
            return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users: {SearchTerm}", searchTerm);
            return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResult("An error occurred while searching users"));
        }
    }

    [HttpGet("inactive")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetInactiveUsers([FromQuery] int daysSinceLastLogin = 30)
    {
        try
        {
            var users = await _userManagementService.GetInactiveUsersAsync(daysSinceLastLogin);
            return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inactive users");
            return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResult("An error occurred while getting inactive users"));
        }
    }

    [HttpPost("bulk-update-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkUpdateUserStatus([FromBody] BulkUpdateUserStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _userManagementService.BulkUpdateUserStatusAsync(request.UserIds, request.IsActive, currentUserId);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating user status");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while bulk updating user status"));
        }
    }

    [HttpPost("bulk-assign-roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkAssignRoles([FromBody] BulkAssignRolesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _userManagementService.BulkAssignRolesAsync(request.UserIds, request.Roles, currentUserId);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning roles");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while bulk assigning roles"));
        }
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkDeleteUsers([FromBody] BulkDeleteUsersRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _userManagementService.BulkDeleteUsersAsync(request.UserIds, currentUserId);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting users");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while bulk deleting users"));
        }
    }

    [HttpGet("{userId}/activity")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<AuditLog>>>> GetUserActivity(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var activities = await _userManagementService.GetUserActivityAsync(userId, page, pageSize);
            return Ok(ApiResponse<List<AuditLog>>.SuccessResult(activities));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity: {UserId}", userId);
            return StatusCode(500, ApiResponse<List<AuditLog>>.ErrorResult("An error occurred while getting user activity"));
        }
    }

    [HttpPost("validate-password")]
    public ActionResult<ApiResponse<PasswordValidationResult>> ValidatePassword([FromBody] ValidatePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<PasswordValidationResult>.ErrorResult("Invalid request data"));
            }

            var result = _passwordPolicyService.ValidatePassword(request.Password, request.Username, request.Email);
            return Ok(ApiResponse<PasswordValidationResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password");
            return StatusCode(500, ApiResponse<PasswordValidationResult>.ErrorResult("An error occurred while validating password"));
        }
    }

    [HttpGet("password-requirements")]
    public ActionResult<ApiResponse<List<string>>> GetPasswordRequirements()
    {
        try
        {
            var requirements = _passwordPolicyService.GetPasswordRequirements();
            return Ok(ApiResponse<List<string>>.SuccessResult(requirements));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password requirements");
            return StatusCode(500, ApiResponse<List<string>>.ErrorResult("An error occurred while getting password requirements"));
        }
    }

    [HttpPost("check-password-expiry")]
    public ActionResult<ApiResponse<PasswordExpiryInfo>> CheckPasswordExpiry([FromBody] CheckPasswordExpiryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<PasswordExpiryInfo>.ErrorResult("Invalid request data"));
            }

            var isExpired = _passwordPolicyService.IsPasswordExpired(request.PasswordChangedAt, request.MaxAgeDays);
            var daysUntilExpiry = _passwordPolicyService.GetDaysUntilPasswordExpiry(request.PasswordChangedAt, request.MaxAgeDays);

            var result = new PasswordExpiryInfo
            {
                IsExpired = isExpired,
                DaysUntilExpiry = daysUntilExpiry,
                PasswordChangedAt = request.PasswordChangedAt,
                MaxAgeDays = request.MaxAgeDays
            };

            return Ok(ApiResponse<PasswordExpiryInfo>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking password expiry");
            return StatusCode(500, ApiResponse<PasswordExpiryInfo>.ErrorResult("An error occurred while checking password expiry"));
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

// Additional request DTOs for bulk operations
public class BulkUpdateUserStatusRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public bool IsActive { get; set; }
}

public class BulkAssignRolesRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public List<string> Roles { get; set; } = new();
}

public class BulkDeleteUsersRequest
{
    public List<Guid> UserIds { get; set; } = new();
}

// Password Policy DTOs
public class ValidatePasswordRequest
{
    public string Password { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
}

public class CheckPasswordExpiryRequest
{
    public DateTime? PasswordChangedAt { get; set; }
    public int MaxAgeDays { get; set; } = 90;
}

public class PasswordExpiryInfo
{
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public int MaxAgeDays { get; set; }
}
