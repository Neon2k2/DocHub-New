using DocHub.API.DTOs;
using DocHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<AuthResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "LOGIN_FAILED",
                        Message = result.Message ?? "Login failed"
                    }
                });
            }

            return Ok(new ApiResponse<AuthResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new ApiResponse<AuthResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<AuthResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "REGISTRATION_FAILED",
                        Message = result.Message ?? "Registration failed"
                    }
                });
            }

            return Ok(new ApiResponse<AuthResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new ApiResponse<AuthResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        try
        {
            // In a real application, you might want to blacklist the token
            // For now, we'll just return success as the client will clear local storage
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("userid")?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Invalid user token"
                    }
                });
            }

            var success = await _authService.ChangePasswordAsync(userId, request);
            
            return Ok(new ApiResponse<bool>
            {
                Success = success,
                Data = success,
                Error = success ? null : new ApiError
                {
                    Code = "PASSWORD_CHANGE_FAILED",
                    Message = "Failed to change password. Please check your current password."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserSummary>>> GetCurrentUser()
    {
        try
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ApiResponse<UserSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "UNAUTHORIZED",
                        Message = "No token provided"
                    }
                });
            }

            var user = await _authService.GetUserFromTokenAsync(token);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<UserSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Invalid token"
                    }
                });
            }

            return Ok(new ApiResponse<UserSummary>
            {
                Success = true,
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new ApiResponse<UserSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }

    [HttpGet("modules")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetUserModules()
    {
        try
        {
            var userIdClaim = User.FindFirst("userid")?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ApiResponse<List<string>>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Invalid user token"
                    }
                });
            }

            var modules = await _authService.GetUserModuleAccessAsync(userId);
            
            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = modules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user modules");
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }
}
