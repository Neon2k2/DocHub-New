using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Users;

// Session Management DTOs
public class UserSessionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("userEmail")]
    public string UserEmail { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("browserName")]
    public string? BrowserName { get; set; }

    [JsonPropertyName("operatingSystem")]
    public string? OperatingSystem { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastActivityAt")]
    public DateTime LastActivityAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("loggedOutAt")]
    public DateTime? LoggedOutAt { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isExpired")]
    public bool IsExpired { get; set; }

    [JsonPropertyName("sessionDuration")]
    public TimeSpan SessionDuration { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

public class GetSessionsRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("userId")]
    public Guid? UserId { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "lastActivityAt";

    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "desc";
}

public class SessionStatsDto
{
    [JsonPropertyName("totalSessions")]
    public int TotalSessions { get; set; }

    [JsonPropertyName("activeSessions")]
    public int ActiveSessions { get; set; }

    [JsonPropertyName("expiredSessions")]
    public int ExpiredSessions { get; set; }

    [JsonPropertyName("sessionsByDeviceType")]
    public Dictionary<string, int> SessionsByDeviceType { get; set; } = new();

    [JsonPropertyName("sessionsByBrowser")]
    public Dictionary<string, int> SessionsByBrowser { get; set; } = new();

    [JsonPropertyName("sessionsByOperatingSystem")]
    public Dictionary<string, int> SessionsByOperatingSystem { get; set; } = new();

    [JsonPropertyName("averageSessionDuration")]
    public TimeSpan AverageSessionDuration { get; set; }

    [JsonPropertyName("longestActiveSession")]
    public TimeSpan LongestActiveSession { get; set; }
}

public class TerminateSessionRequest
{
    [Required]
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class TerminateAllSessionsRequest
{
    [Required]
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("excludeCurrentSession")]
    public bool ExcludeCurrentSession { get; set; } = true;
}

public class RefreshTokenDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    [JsonPropertyName("revokedByIp")]
    public string? RevokedByIp { get; set; }

    [JsonPropertyName("isExpired")]
    public bool IsExpired { get; set; }

    [JsonPropertyName("isRevoked")]
    public bool IsRevoked { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public class RevokeRefreshTokenRequest
{
    [Required]
    [JsonPropertyName("tokenId")]
    public Guid TokenId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class RevokeAllRefreshTokensRequest
{
    [Required]
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
