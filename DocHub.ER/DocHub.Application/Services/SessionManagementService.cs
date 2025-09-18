using DocHub.Core.Interfaces;
using DocHub.Core.Entities;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace DocHub.Application.Services;

public class SessionManagementService : ISessionManagementService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<SessionManagementService> _logger;

    public SessionManagementService(IDbContext dbContext, ILogger<SessionManagementService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserSessionDto> CreateSessionAsync(Guid userId, string sessionToken, string refreshToken, string ipAddress, string userAgent, string? deviceName = null)
    {
        try
        {
            var deviceInfo = ParseUserAgent(userAgent);
            var expiresAt = DateTime.UtcNow.AddHours(24); // 24 hour session

            var session = new UserSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                RefreshToken = refreshToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceName = deviceName ?? deviceInfo.DeviceName,
                DeviceType = deviceInfo.DeviceType,
                BrowserName = deviceInfo.BrowserName,
                OperatingSystem = deviceInfo.OperatingSystem,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsActive = true
            };

            await _dbContext.UserSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();

            return MapToUserSessionDto(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateSessionActivityAsync(Guid sessionId, string ipAddress, string userAgent)
    {
        try
        {
            var session = await _dbContext.UserSessions.FindAsync(sessionId);
            if (session == null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow || session.LoggedOutAt != null)
            {
                return false;
            }

            session.LastActivityAt = DateTime.UtcNow;
            session.IpAddress = ipAddress;
            session.UserAgent = userAgent;

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session activity: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId, string reason, string currentUserId)
    {
        try
        {
            var session = await _dbContext.UserSessions.FindAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            session.IsActive = false;
            session.LoggedOutAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogSessionActivityAsync(session.UserId, "SESSION_TERMINATED", $"Session terminated by {currentUserId}. Reason: {reason}", session.IpAddress, session.UserAgent);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> TerminateAllUserSessionsAsync(Guid userId, string reason, string currentUserId, bool excludeCurrentSession = true)
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (excludeCurrentSession)
            {
                // Find current session by checking recent activity
                var currentSession = sessions
                    .Where(s => s.LastActivityAt > DateTime.UtcNow.AddMinutes(-5))
                    .OrderByDescending(s => s.LastActivityAt)
                    .FirstOrDefault();

                if (currentSession != null)
                {
                    sessions.Remove(currentSession);
                }
            }

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogSessionActivityAsync(userId, "ALL_SESSIONS_TERMINATED", $"All sessions terminated by {currentUserId}. Reason: {reason}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating all user sessions: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> TerminateExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Terminated {Count} expired sessions", expiredSessions.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating expired sessions");
            throw;
        }
    }

    public async Task<PaginatedResponse<UserSessionDto>> GetSessionsAsync(GetSessionsRequest request)
    {
        try
        {
            var query = _dbContext.UserSessions
                .Include(s => s.User)
                .AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
            {
                query = query.Where(s => s.UserId == request.UserId.Value);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(request.DeviceType))
            {
                query = query.Where(s => s.DeviceType == request.DeviceType);
            }

            if (!string.IsNullOrEmpty(request.IpAddress))
            {
                query = query.Where(s => s.IpAddress == request.IpAddress);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "createdat" => request.SortDirection == "asc" ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt),
                "lastactivity" => request.SortDirection == "asc" ? query.OrderBy(s => s.LastActivityAt) : query.OrderByDescending(s => s.LastActivityAt),
                "expiresat" => request.SortDirection == "asc" ? query.OrderBy(s => s.ExpiresAt) : query.OrderByDescending(s => s.ExpiresAt),
                "username" => request.SortDirection == "asc" ? query.OrderBy(s => s.User.Username) : query.OrderByDescending(s => s.User.Username),
                _ => request.SortDirection == "asc" ? query.OrderBy(s => s.LastActivityAt) : query.OrderByDescending(s => s.LastActivityAt)
            };

            var totalCount = await query.CountAsync();
            var sessions = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => MapToUserSessionDto(s))
                .ToListAsync();

            return new PaginatedResponse<UserSessionDto>
            {
                Items = sessions,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            throw;
        }
    }

    public async Task<UserSessionDto?> GetSessionByIdAsync(Guid sessionId)
    {
        try
        {
            var session = await _dbContext.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            return session != null ? MapToUserSessionDto(session) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session by ID: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetUserSessionsAsync(Guid userId, bool activeOnly = true)
    {
        try
        {
            var query = _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId);

            if (activeOnly)
            {
                query = query.Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null);
            }

            var sessions = await query
                .OrderByDescending(s => s.LastActivityAt)
                .Select(s => MapToUserSessionDto(s))
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetActiveSessionsAsync()
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();

            return sessions.Select(MapToUserSessionDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetExpiredSessionsAsync()
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.ExpiresAt <= DateTime.UtcNow || s.LoggedOutAt != null)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();

            return sessions.Select(MapToUserSessionDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired sessions");
            throw;
        }
    }

    public async Task<SessionStatsDto> GetSessionStatsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var totalSessions = await _dbContext.UserSessions.CountAsync();
            var activeSessions = await _dbContext.UserSessions.CountAsync(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null);
            var expiredSessions = await _dbContext.UserSessions.CountAsync(s => s.ExpiresAt <= DateTime.UtcNow || s.LoggedOutAt != null);

            // Today's sessions
            var totalSessionsToday = await _dbContext.UserSessions.CountAsync(s => s.CreatedAt >= today);
            var uniqueUsersToday = await _dbContext.UserSessions
                .Where(s => s.CreatedAt >= today)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // This week's sessions
            var totalSessionsThisWeek = await _dbContext.UserSessions.CountAsync(s => s.CreatedAt >= weekStart);
            var uniqueUsersThisWeek = await _dbContext.UserSessions
                .Where(s => s.CreatedAt >= weekStart)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // This month's sessions
            var totalSessionsThisMonth = await _dbContext.UserSessions.CountAsync(s => s.CreatedAt >= monthStart);
            var uniqueUsersThisMonth = await _dbContext.UserSessions
                .Where(s => s.CreatedAt >= monthStart)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            var sessionsByDeviceType = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .GroupBy(s => s.DeviceType ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var sessionsByBrowser = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .GroupBy(s => s.BrowserName ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var sessionsByOS = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .GroupBy(s => s.OperatingSystem ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Sessions by department
            var sessionsByDepartment = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .Include(s => s.User)
                .GroupBy(s => s.User.Department ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Sessions by hour (for today)
            var sessionsByHour = await _dbContext.UserSessions
                .Where(s => s.CreatedAt >= today)
                .GroupBy(s => s.CreatedAt.Hour)
                .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count());

            // Get active sessions and calculate durations in memory
            var activeSessionsForStats = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .Select(s => new { s.LastActivityAt, s.CreatedAt })
                .ToListAsync();

            var durations = activeSessionsForStats
                .Select(s => s.LastActivityAt - s.CreatedAt)
                .ToList();

            var averageDuration = durations.Any() ? TimeSpan.FromSeconds(durations.Average(d => d.TotalSeconds)) : TimeSpan.Zero;
            var longestSession = durations.Any() ? durations.Max() : TimeSpan.Zero;

            return new SessionStatsDto
            {
                TotalSessions = totalSessions,
                TotalActiveSessions = activeSessions,
                ActiveSessions = activeSessions,
                ExpiredSessions = expiredSessions,
                TotalSessionsToday = totalSessionsToday,
                TotalSessionsThisWeek = totalSessionsThisWeek,
                TotalSessionsThisMonth = totalSessionsThisMonth,
                UniqueUsersToday = uniqueUsersToday,
                UniqueUsersThisWeek = uniqueUsersThisWeek,
                UniqueUsersThisMonth = uniqueUsersThisMonth,
                SessionsByDeviceType = sessionsByDeviceType,
                SessionsByBrowser = sessionsByBrowser,
                SessionsByOperatingSystem = sessionsByOS,
                SessionsByDepartment = sessionsByDepartment,
                SessionsByHour = sessionsByHour,
                AverageSessionDuration = FormatDuration(averageDuration),
                LongestActiveSession = FormatDuration(longestSession)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session stats");
            throw;
        }
    }

    public async Task<int> GetActiveSessionCountAsync()
    {
        try
        {
            return await _dbContext.UserSessions
                .CountAsync(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active session count");
            throw;
        }
    }

    public async Task<int> GetUserActiveSessionCountAsync(Guid userId)
    {
        try
        {
            return await _dbContext.UserSessions
                .CountAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user active session count: {UserId}", userId);
            throw;
        }
    }

    public async Task<TimeSpan> GetAverageSessionDurationAsync()
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .Select(s => new { s.LastActivityAt, s.CreatedAt })
                .ToListAsync();

            var durations = sessions.Select(s => s.LastActivityAt - s.CreatedAt).ToList();
            return durations.Any() ? TimeSpan.FromSeconds(durations.Average(d => d.TotalSeconds)) : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting average session duration");
            throw;
        }
    }

    public async Task<TimeSpan> GetLongestActiveSessionAsync()
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .Select(s => new { s.LastActivityAt, s.CreatedAt })
                .ToListAsync();

            var durations = sessions.Select(s => s.LastActivityAt - s.CreatedAt).ToList();
            return durations.Any() ? durations.Max() : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting longest active session");
            throw;
        }
    }

    public async Task<bool> IsSessionValidAsync(Guid sessionId)
    {
        try
        {
            var session = await _dbContext.UserSessions.FindAsync(sessionId);
            return session != null && session.IsActive && session.ExpiresAt > DateTime.UtcNow && session.LoggedOutAt == null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId)
    {
        try
        {
            var session = await _dbContext.UserSessions.FindAsync(sessionId);
            return session != null && session.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if session is active: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> ValidateSessionTokenAsync(string sessionToken)
    {
        try
        {
            var session = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
            
            return session != null && session.IsActive && session.ExpiresAt > DateTime.UtcNow && session.LoggedOutAt == null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session token");
            throw;
        }
    }

    public async Task<UserSessionDto?> GetSessionByTokenAsync(string sessionToken)
    {
        try
        {
            var session = await _dbContext.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            return session != null ? MapToUserSessionDto(session) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session by token");
            throw;
        }
    }

    public async Task<RefreshTokenDto> CreateRefreshTokenAsync(Guid userId, string token, DateTime expiresAt)
    {
        try
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            await _dbContext.RefreshTokens.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();

            return MapToRefreshTokenDto(refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(Guid tokenId, string reason, string currentUserId)
    {
        try
        {
            var token = await _dbContext.RefreshTokens.FindAsync(tokenId);
            if (token == null)
            {
                return false;
            }

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ""; // TODO: Get IP from context

            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogSessionActivityAsync(token.UserId, "REFRESH_TOKEN_REVOKED", $"Refresh token revoked by {currentUserId}. Reason: {reason}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token: {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<bool> RevokeAllUserRefreshTokensAsync(Guid userId, string reason, string currentUserId)
    {
        try
        {
            var tokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.IsActive)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ""; // TODO: Get IP from context
            }

            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogSessionActivityAsync(userId, "ALL_REFRESH_TOKENS_REVOKED", $"All refresh tokens revoked by {currentUserId}. Reason: {reason}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user refresh tokens: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RevokeExpiredRefreshTokensAsync()
    {
        try
        {
            var expiredTokens = await _dbContext.RefreshTokens
                .Where(t => t.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in expiredTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Revoked {Count} expired refresh tokens", expiredTokens.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking expired refresh tokens");
            throw;
        }
    }

    public async Task<RefreshTokenDto?> GetRefreshTokenAsync(string token)
    {
        try
        {
            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            return refreshToken != null ? MapToRefreshTokenDto(refreshToken) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refresh token");
            throw;
        }
    }

    public async Task<List<RefreshTokenDto>> GetUserRefreshTokensAsync(Guid userId, bool activeOnly = true)
    {
        try
        {
            var query = _dbContext.RefreshTokens
                .Where(t => t.UserId == userId);

            if (activeOnly)
            {
                query = query.Where(t => t.IsActive);
            }

            var tokens = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => MapToRefreshTokenDto(t))
                .ToListAsync();

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user refresh tokens: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsSuspiciousActivityAsync(string ipAddress, Guid userId)
    {
        try
        {
            // Check for multiple failed login attempts from same IP
            var recentFailedAttempts = await _dbContext.AuditLogs
                .Where(a => a.IpAddress == ipAddress && 
                           a.Action == "LOGIN_FAILED" && 
                           a.CreatedAt > DateTime.UtcNow.AddHours(1))
                .CountAsync();

            // Check for multiple sessions from different locations
            var recentSessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && 
                           s.CreatedAt > DateTime.UtcNow.AddHours(24) &&
                           s.IpAddress != ipAddress)
                .CountAsync();

            return recentFailedAttempts > 5 || recentSessions > 3;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking suspicious activity");
            throw;
        }
    }

    public async Task<bool> BlockSuspiciousSessionsAsync(string ipAddress)
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Where(s => s.IpAddress == ipAddress && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("Blocked {Count} suspicious sessions from IP: {IpAddress}", sessions.Count, ipAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking suspicious sessions");
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetSessionsByIpAddressAsync(string ipAddress)
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.IpAddress == ipAddress)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => MapToUserSessionDto(s))
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions by IP address: {IpAddress}", ipAddress);
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetSessionsByDeviceTypeAsync(string deviceType)
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.DeviceType == deviceType)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => MapToUserSessionDto(s))
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions by device type: {DeviceType}", deviceType);
            throw;
        }
    }

    public async Task<bool> CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _dbContext.UserSessions
                .Where(s => s.ExpiresAt <= DateTime.UtcNow || s.LoggedOutAt != null)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            throw;
        }
    }

    public async Task<bool> CleanupOldSessionsAsync(int daysOld = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var oldSessions = await _dbContext.UserSessions
                .Where(s => s.CreatedAt < cutoffDate)
                .ToListAsync();

            foreach (var session in oldSessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old sessions", oldSessions.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old sessions");
            throw;
        }
    }

    public async Task<bool> CleanupInactiveSessionsAsync(int hoursInactive = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hoursInactive);
            var inactiveSessions = await _dbContext.UserSessions
                .Where(s => s.IsActive && s.LastActivityAt < cutoffTime)
                .ToListAsync();

            foreach (var session in inactiveSessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} inactive sessions", inactiveSessions.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up inactive sessions");
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetConcurrentSessionsAsync(Guid userId)
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();

            return sessions.Select(MapToUserSessionDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting concurrent sessions: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> EnforceSessionLimitAsync(Guid userId, int maxSessions = 5)
    {
        try
        {
            var activeSessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow && s.LoggedOutAt == null)
                .OrderBy(s => s.LastActivityAt)
                .ToListAsync();

            if (activeSessions.Count <= maxSessions)
            {
                return true;
            }

            // Terminate oldest sessions
            var sessionsToTerminate = activeSessions.Take(activeSessions.Count - maxSessions);
            foreach (var session in sessionsToTerminate)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Enforced session limit for user {UserId}, terminated {Count} sessions", userId, sessionsToTerminate.Count());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing session limit: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserSessionDto>> GetSessionsByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.CreatedAt >= startTime && s.CreatedAt <= endTime)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => MapToUserSessionDto(s))
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions by time range");
            throw;
        }
    }

    // Helper methods
    private UserSessionDto MapToUserSessionDto(UserSession session)
    {
        return new UserSessionDto
        {
            Id = session.Id,
            SessionId = session.Id.ToString(),
            UserId = session.UserId,
            UserName = session.User?.Username ?? "Unknown",
            UserEmail = session.User?.Email ?? "Unknown",
            FirstName = session.User?.FirstName ?? "Unknown",
            LastName = session.User?.LastName ?? "Unknown",
            Department = session.User?.Department ?? "Unknown",
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent,
            DeviceName = session.DeviceName,
            DeviceType = session.DeviceType,
            BrowserName = session.BrowserName,
            OperatingSystem = session.OperatingSystem,
            DeviceInfo = $"{session.DeviceType} - {session.BrowserName}",
            CreatedAt = session.CreatedAt,
            LoginAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            ExpiresAt = session.ExpiresAt,
            LoggedOutAt = session.LoggedOutAt,
            IsActive = session.IsActive,
            IsExpired = session.IsExpired,
            SessionDuration = session.LastActivityAt - session.CreatedAt,
            Location = GetLocationFromIp(session.IpAddress)
        };
    }

    private RefreshTokenDto MapToRefreshTokenDto(RefreshToken token)
    {
        return new RefreshTokenDto
        {
            Id = token.Id,
            UserId = token.UserId,
            Token = token.Token,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt,
            RevokedAt = token.RevokedAt,
            RevokedByIp = token.RevokedByIp,
            IsExpired = token.IsExpired,
            IsRevoked = token.IsRevoked,
            IsActive = token.IsActive
        };
    }

    private (string DeviceName, string DeviceType, string BrowserName, string OperatingSystem) ParseUserAgent(string userAgent)
    {
        // Simplified user agent parsing - in production, use a proper library
        var deviceType = "Desktop";
        var browserName = "Unknown";
        var operatingSystem = "Unknown";

        if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
        {
            deviceType = "Mobile";
        }
        else if (userAgent.Contains("Tablet") || userAgent.Contains("iPad"))
        {
            deviceType = "Tablet";
        }

        if (userAgent.Contains("Chrome"))
            browserName = "Chrome";
        else if (userAgent.Contains("Firefox"))
            browserName = "Firefox";
        else if (userAgent.Contains("Safari"))
            browserName = "Safari";
        else if (userAgent.Contains("Edge"))
            browserName = "Edge";

        if (userAgent.Contains("Windows"))
            operatingSystem = "Windows";
        else if (userAgent.Contains("Mac"))
            operatingSystem = "macOS";
        else if (userAgent.Contains("Linux"))
            operatingSystem = "Linux";
        else if (userAgent.Contains("Android"))
            operatingSystem = "Android";
        else if (userAgent.Contains("iOS"))
            operatingSystem = "iOS";

        return (deviceType, deviceType, browserName, operatingSystem);
    }

    private string? GetLocationFromIp(string ipAddress)
    {
        // TODO: Implement IP geolocation lookup
        return null;
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return "0m";

        var totalHours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        var seconds = duration.Seconds;

        if (totalHours > 0)
            return $"{totalHours}h {minutes}m";
        else if (minutes > 0)
            return $"{minutes}m {seconds}s";
        else
            return $"{seconds}s";
    }

    private async Task<bool> LogSessionActivityAsync(Guid userId, string action, string details, string ipAddress, string userAgent)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.AddAsync(auditLog);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging session activity: {UserId}", userId);
            return false;
        }
    }

    public async Task<List<LoginHistoryDto>> GetUserLoginHistoryAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            var sessions = await _dbContext.UserSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var loginHistory = sessions.Select(session => new LoginHistoryDto
            {
                Id = session.Id,
                UserId = session.UserId,
                UserName = session.User?.Username ?? "Unknown",
                UserEmail = session.User?.Email ?? "Unknown",
                IpAddress = session.IpAddress,
                UserAgent = session.UserAgent,
                DeviceName = session.DeviceName,
                DeviceType = session.DeviceType,
                BrowserName = session.BrowserName,
                OperatingSystem = session.OperatingSystem,
                LoginAt = session.CreatedAt,
                LogoutAt = session.LoggedOutAt,
                SessionDuration = session.LoggedOutAt.HasValue ? 
                    session.LoggedOutAt.Value - session.CreatedAt : 
                    DateTime.UtcNow - session.CreatedAt,
                IsSuccessful = true, // Assuming all sessions in the table are successful
                FailureReason = null,
                Location = null // Could be enhanced with IP geolocation
            }).ToList();

            return loginHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user login history: {UserId}", userId);
            throw;
        }
    }
}
