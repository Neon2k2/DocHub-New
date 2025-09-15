using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionManagementController : ControllerBase
{
    private readonly ISessionManagementService _sessionManagementService;
    private readonly ILogger<SessionManagementController> _logger;

    public SessionManagementController(ISessionManagementService sessionManagementService, ILogger<SessionManagementController> logger)
    {
        _sessionManagementService = sessionManagementService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserSessionDto>>>> GetSessions([FromQuery] GetSessionsRequest request)
    {
        try
        {
            var sessions = await _sessionManagementService.GetSessionsAsync(request);
            return Ok(ApiResponse<PaginatedResponse<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, ApiResponse<PaginatedResponse<UserSessionDto>>.ErrorResult("An error occurred while getting sessions"));
        }
    }

    [HttpGet("{sessionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> GetSession(Guid sessionId)
    {
        try
        {
            var session = await _sessionManagementService.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                return NotFound(ApiResponse<UserSessionDto>.ErrorResult("Session not found"));
            }

            return Ok(ApiResponse<UserSessionDto>.SuccessResult(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session: {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<UserSessionDto>.ErrorResult("An error occurred while getting session"));
        }
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetUserSessions(Guid userId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var sessions = await _sessionManagementService.GetUserSessionsAsync(userId, activeOnly);
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions: {UserId}", userId);
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting user sessions"));
        }
    }

    [HttpGet("active")]
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

    [HttpGet("expired")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetExpiredSessions()
    {
        try
        {
            var sessions = await _sessionManagementService.GetExpiredSessionsAsync();
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired sessions");
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting expired sessions"));
        }
    }

    [HttpGet("stats")]
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
            _logger.LogError(ex, "Error getting session stats");
            return StatusCode(500, ApiResponse<SessionStatsDto>.ErrorResult("An error occurred while getting session stats"));
        }
    }

    [HttpGet("count/active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> GetActiveSessionCount()
    {
        try
        {
            var count = await _sessionManagementService.GetActiveSessionCountAsync();
            return Ok(ApiResponse<int>.SuccessResult(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active session count");
            return StatusCode(500, ApiResponse<int>.ErrorResult("An error occurred while getting active session count"));
        }
    }

    [HttpGet("count/user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> GetUserActiveSessionCount(Guid userId)
    {
        try
        {
            var count = await _sessionManagementService.GetUserActiveSessionCountAsync(userId);
            return Ok(ApiResponse<int>.SuccessResult(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user active session count: {UserId}", userId);
            return StatusCode(500, ApiResponse<int>.ErrorResult("An error occurred while getting user active session count"));
        }
    }

    [HttpPost("{sessionId}/terminate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> TerminateSession(Guid sessionId, [FromBody] TerminateSessionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _sessionManagementService.TerminateSessionAsync(sessionId, request.Reason ?? "Terminated by admin", currentUserId);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Session not found"));
            }

            return Ok(ApiResponse<bool>.SuccessResult(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session: {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while terminating session"));
        }
    }

    [HttpPost("user/{userId}/terminate-all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> TerminateAllUserSessions(Guid userId, [FromBody] TerminateAllSessionsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _sessionManagementService.TerminateAllUserSessionsAsync(userId, request.Reason ?? "Terminated by admin", currentUserId, request.ExcludeCurrentSession);
            
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating all user sessions: {UserId}", userId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while terminating all user sessions"));
        }
    }

    [HttpPost("terminate-expired")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> TerminateExpiredSessions()
    {
        try
        {
            var result = await _sessionManagementService.TerminateExpiredSessionsAsync();
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating expired sessions");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while terminating expired sessions"));
        }
    }

    [HttpPost("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> CleanupSessions([FromQuery] int? daysOld = null, [FromQuery] int? hoursInactive = null)
    {
        try
        {
            bool result = true;

            if (daysOld.HasValue)
            {
                result &= await _sessionManagementService.CleanupOldSessionsAsync(daysOld.Value);
            }

            if (hoursInactive.HasValue)
            {
                result &= await _sessionManagementService.CleanupInactiveSessionsAsync(hoursInactive.Value);
            }

            if (!daysOld.HasValue && !hoursInactive.HasValue)
            {
                result = await _sessionManagementService.CleanupExpiredSessionsAsync();
            }

            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up sessions");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while cleaning up sessions"));
        }
    }

    [HttpGet("concurrent/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetConcurrentSessions(Guid userId)
    {
        try
        {
            var sessions = await _sessionManagementService.GetConcurrentSessionsAsync(userId);
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting concurrent sessions: {UserId}", userId);
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting concurrent sessions"));
        }
    }

    [HttpPost("enforce-limit/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> EnforceSessionLimit(Guid userId, [FromQuery] int maxSessions = 5)
    {
        try
        {
            var result = await _sessionManagementService.EnforceSessionLimitAsync(userId, maxSessions);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing session limit: {UserId}", userId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while enforcing session limit"));
        }
    }

    [HttpGet("by-ip/{ipAddress}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetSessionsByIpAddress(string ipAddress)
    {
        try
        {
            var sessions = await _sessionManagementService.GetSessionsByIpAddressAsync(ipAddress);
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions by IP address: {IpAddress}", ipAddress);
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting sessions by IP address"));
        }
    }

    [HttpGet("by-device/{deviceType}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetSessionsByDeviceType(string deviceType)
    {
        try
        {
            var sessions = await _sessionManagementService.GetSessionsByDeviceTypeAsync(deviceType);
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions by device type: {DeviceType}", deviceType);
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting sessions by device type"));
        }
    }

    [HttpGet("by-time-range")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetSessionsByTimeRange([FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
    {
        try
        {
            var sessions = await _sessionManagementService.GetSessionsByTimeRangeAsync(startTime, endTime);
            return Ok(ApiResponse<List<UserSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions by time range");
            return StatusCode(500, ApiResponse<List<UserSessionDto>>.ErrorResult("An error occurred while getting sessions by time range"));
        }
    }

    [HttpPost("block-suspicious/{ipAddress}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockSuspiciousSessions(string ipAddress)
    {
        try
        {
            var result = await _sessionManagementService.BlockSuspiciousSessionsAsync(ipAddress);
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking suspicious sessions: {IpAddress}", ipAddress);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while blocking suspicious sessions"));
        }
    }

    [HttpGet("refresh-tokens/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<RefreshTokenDto>>>> GetUserRefreshTokens(Guid userId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var tokens = await _sessionManagementService.GetUserRefreshTokensAsync(userId, activeOnly);
            return Ok(ApiResponse<List<RefreshTokenDto>>.SuccessResult(tokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user refresh tokens: {UserId}", userId);
            return StatusCode(500, ApiResponse<List<RefreshTokenDto>>.ErrorResult("An error occurred while getting user refresh tokens"));
        }
    }

    [HttpPost("refresh-tokens/{tokenId}/revoke")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeRefreshToken(Guid tokenId, [FromBody] RevokeRefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _sessionManagementService.RevokeRefreshTokenAsync(tokenId, request.Reason ?? "Revoked by admin", currentUserId);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Refresh token not found"));
            }

            return Ok(ApiResponse<bool>.SuccessResult(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token: {TokenId}", tokenId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while revoking refresh token"));
        }
    }

    [HttpPost("refresh-tokens/user/{userId}/revoke-all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeAllUserRefreshTokens(Guid userId, [FromBody] RevokeAllRefreshTokensRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid request data"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _sessionManagementService.RevokeAllUserRefreshTokensAsync(userId, request.Reason ?? "Revoked by admin", currentUserId);
            
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user refresh tokens: {UserId}", userId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while revoking all user refresh tokens"));
        }
    }

    [HttpPost("refresh-tokens/revoke-expired")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeExpiredRefreshTokens()
    {
        try
        {
            var result = await _sessionManagementService.RevokeExpiredRefreshTokensAsync();
            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking expired refresh tokens");
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while revoking expired refresh tokens"));
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
