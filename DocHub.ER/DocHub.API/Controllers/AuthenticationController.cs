using DocHub.Application.Services;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Auth;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/auth")] // Additional route for frontend compatibility
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(IAuthenticationService authService, IUserRepository userRepository, ILogger<AuthenticationController> logger)
    {
        _authService = authService;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid request data"));
            }

            var response = await _authService.LoginAsync(request);
            return Ok(ApiResponse<LoginResponse>.SuccessResult(response));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("An error occurred during login"));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserAsync(userId);
            return Ok(user);
        }
        catch (ArgumentException)
        {
            return NotFound(new { message = "User not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred while getting user information" });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (!success)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (ArgumentException)
        {
            return NotFound(new { message = "User not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _authService.ResetPasswordAsync(request.Email);
            return Ok(new { message = "If the email exists, a password reset link has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password");
            return StatusCode(500, new { message = "An error occurred while processing forgot password request" });
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _authService.ConfirmPasswordResetAsync(request.Email, request.Token, request.NewPassword);
            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired reset token" });
            }

            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An error occurred while resetting password" });
        }
    }

    [HttpGet("debug/users")]
    public async Task<ActionResult> GetUsersForDebug()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var userInfo = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.IsActive,
                u.CreatedAt,
                PasswordHashLength = u.PasswordHash?.Length ?? 0,
                PasswordHashPrefix = u.PasswordHash?.Substring(0, Math.Min(10, u.PasswordHash?.Length ?? 0)) ?? ""
            });

            return Ok(new { 
                TotalUsers = users.Count(), 
                Users = userInfo 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for debug");
            return StatusCode(500, new { message = "An error occurred while getting users" });
        }
    }

    [HttpPost("debug/test-password")]
    public ActionResult TestPassword([FromBody] dynamic request)
    {
        try
        {
            string password = request.password;
            string hash = request.hash;
            
            var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
            
            return Ok(new { 
                Password = password,
                HashPrefix = hash?.Substring(0, Math.Min(10, hash?.Length ?? 0)) ?? "",
                IsValid = isValid
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing password");
            return StatusCode(500, new { message = "An error occurred while testing password" });
        }
    }
}
