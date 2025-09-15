using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;

namespace DocHub.Core.Interfaces;

public interface ISessionManagementService
{
    // Session Management
    Task<UserSessionDto> CreateSessionAsync(Guid userId, string sessionToken, string refreshToken, string ipAddress, string userAgent, string? deviceName = null);
    Task<bool> UpdateSessionActivityAsync(Guid sessionId, string ipAddress, string userAgent);
    Task<bool> TerminateSessionAsync(Guid sessionId, string reason, string currentUserId);
    Task<bool> TerminateAllUserSessionsAsync(Guid userId, string reason, string currentUserId, bool excludeCurrentSession = true);
    Task<bool> TerminateExpiredSessionsAsync();

    // Session Queries
    Task<PaginatedResponse<UserSessionDto>> GetSessionsAsync(GetSessionsRequest request);
    Task<UserSessionDto?> GetSessionByIdAsync(Guid sessionId);
    Task<List<UserSessionDto>> GetUserSessionsAsync(Guid userId, bool activeOnly = true);
    Task<List<UserSessionDto>> GetActiveSessionsAsync();
    Task<List<UserSessionDto>> GetExpiredSessionsAsync();

    // Session Statistics
    Task<SessionStatsDto> GetSessionStatsAsync();
    Task<int> GetActiveSessionCountAsync();
    Task<int> GetUserActiveSessionCountAsync(Guid userId);
    Task<TimeSpan> GetAverageSessionDurationAsync();
    Task<TimeSpan> GetLongestActiveSessionAsync();

    // Session Validation
    Task<bool> IsSessionValidAsync(Guid sessionId);
    Task<bool> IsSessionActiveAsync(Guid sessionId);
    Task<bool> ValidateSessionTokenAsync(string sessionToken);
    Task<UserSessionDto?> GetSessionByTokenAsync(string sessionToken);

    // Refresh Token Management
    Task<RefreshTokenDto> CreateRefreshTokenAsync(Guid userId, string token, DateTime expiresAt);
    Task<bool> RevokeRefreshTokenAsync(Guid tokenId, string reason, string currentUserId);
    Task<bool> RevokeAllUserRefreshTokensAsync(Guid userId, string reason, string currentUserId);
    Task<bool> RevokeExpiredRefreshTokensAsync();
    Task<RefreshTokenDto?> GetRefreshTokenAsync(string token);
    Task<List<RefreshTokenDto>> GetUserRefreshTokensAsync(Guid userId, bool activeOnly = true);

    // Session Security
    Task<bool> IsSuspiciousActivityAsync(string ipAddress, Guid userId);
    Task<bool> BlockSuspiciousSessionsAsync(string ipAddress);
    Task<List<UserSessionDto>> GetSessionsByIpAddressAsync(string ipAddress);
    Task<List<UserSessionDto>> GetSessionsByDeviceTypeAsync(string deviceType);

    // Session Cleanup
    Task<bool> CleanupExpiredSessionsAsync();
    Task<bool> CleanupOldSessionsAsync(int daysOld = 30);
    Task<bool> CleanupInactiveSessionsAsync(int hoursInactive = 24);

    // Session Monitoring
    Task<List<UserSessionDto>> GetConcurrentSessionsAsync(Guid userId);
    Task<bool> EnforceSessionLimitAsync(Guid userId, int maxSessions = 5);
    Task<List<UserSessionDto>> GetSessionsByTimeRangeAsync(DateTime startTime, DateTime endTime);
}
