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
public class SessionManagementController : ControllerBase
{
    private readonly ISessionManagementService _sessionManagementService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<SessionManagementController> _logger;

    public SessionManagementController(
        ISessionManagementService sessionManagementService,
        IUserManagementService userManagementService,
        ILogger<SessionManagementController> logger)
    {
        _sessionManagementService = sessionManagementService;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    [HttpGet("active-sessions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetActiveSessions()
    {
        try
        {
            var sessions = await _sessionManagementService.GetActiveSessionsAsync();
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting active sessions"));
        }
    }

    [HttpGet("user-sessions/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetUserSessions(Guid userId)
    {
        try
        {
            var sessions = await _sessionManagementService.GetUserSessionsAsync(userId);
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions for {UserId}", userId);
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting user sessions"));
        }
    }

    [HttpGet("my-sessions")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetMySessions()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var sessions = await _sessionManagementService.GetUserSessionsAsync(Guid.Parse(currentUserId));
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user sessions");
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting your sessions"));
        }
    }

    [HttpPost("terminate-session/{sessionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> TerminateSession(Guid sessionId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _sessionManagementService.TerminateSessionAsync(sessionId, "Admin action", currentUserId);
            if (result)
            {
                return Ok(ApiResponse<string>.SuccessResult("Session terminated successfully"));
            }

            return BadRequest(ApiResponse<string>.ErrorResult("Failed to terminate session"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while terminating session"));
        }
    }

    [HttpPost("terminate-user-sessions/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> TerminateUserSessions(Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (userId == Guid.Parse(currentUserId))
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Cannot terminate your own sessions"));
            }

            var result = await _sessionManagementService.TerminateAllUserSessionsAsync(userId, "Admin action", currentUserId, false);
            if (result)
            {
                return Ok(ApiResponse<string>.SuccessResult("All user sessions terminated successfully"));
            }

            return BadRequest(ApiResponse<string>.ErrorResult("Failed to terminate user sessions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating user sessions for {UserId}", userId);
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while terminating user sessions"));
        }
    }

    [HttpPost("terminate-my-other-sessions")]
    public async Task<ActionResult<ApiResponse<string>>> TerminateMyOtherSessions()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentSessionId = GetCurrentSessionId();
            
            var result = await _sessionManagementService.TerminateAllUserSessionsAsync(Guid.Parse(currentUserId), "User requested", currentUserId, true);
            if (result)
            {
                return Ok(ApiResponse<string>.SuccessResult("Other sessions terminated successfully"));
            }

            return BadRequest(ApiResponse<string>.ErrorResult("Failed to terminate other sessions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating current user's other sessions");
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while terminating other sessions"));
        }
    }

    [HttpGet("session-stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SessionStatsDto>>> GetSessionStats()
    {
        try
        {
            var stats = await _sessionManagementService.GetSessionStatsAsync();
            return Ok(ApiResponse<SessionStatsDto>.SuccessResult(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session statistics");
            return StatusCode(500, ApiResponse<SessionStatsDto>.ErrorResult("An error occurred while getting session statistics"));
        }
    }

    [HttpPost("cleanup-expired-sessions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> CleanupExpiredSessions()
    {
        try
        {
            var result = await _sessionManagementService.CleanupExpiredSessionsAsync();
            return Ok(ApiResponse<string>.SuccessResult(result ? "Expired sessions cleanup completed" : "No expired sessions to cleanup"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while cleaning up expired sessions"));
        }
    }

    // [HttpGet("login-history/{userId}")]
    // [Authorize(Roles = "Admin")]
    // public async Task<ActionResult<ApiResponse<List<DocHub.Shared.DTOs.Session.LoginHistoryDto>>>> GetUserLoginHistory(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    // {
    //     try
    //     {
    //         var history = await _sessionManagementService.GetUserLoginHistoryAsync(userId, page, pageSize);
    //         return Ok(ApiResponse<List<DocHub.Shared.DTOs.Session.LoginHistoryDto>>.SuccessResult(history));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting login history for {UserId}", userId);
    //         return StatusCode(500, ApiResponse<List<DocHub.Shared.DTOs.Session.LoginHistoryDto>>.ErrorResult("An error occurred while getting login history"));
    //     }
    // }

    // [HttpGet("my-login-history")]
    // public async Task<ActionResult<ApiResponse<List<DocHub.Shared.DTOs.Session.LoginHistoryDto>>>> GetMyLoginHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    // {
    //     try
    //     {
    //         var currentUserId = GetCurrentUserId();
    //         var history = await _sessionManagementService.GetUserLoginHistoryAsync(Guid.Parse(currentUserId), page, pageSize);
    //         return Ok(ApiResponse<List<DocHub.Shared.DTOs.Session.LoginHistoryDto>>.SuccessResult(history));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting current user's login history");
    //         return StatusCode(500, ApiResponse<List<DocHub.Shared.DTOs.Session.LoginHistoryDto>>.ErrorResult("An error occurred while getting your login history"));
    //     }
    // }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found");
    }

    private string GetCurrentSessionId()
    {
        return User.FindFirst("SessionId")?.Value ?? Guid.NewGuid().ToString();
    }
}