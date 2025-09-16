using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Session;

public class ActiveSessionDto
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("loginAt")]
    public DateTime LoginAt { get; set; }

    [JsonPropertyName("lastActivityAt")]
    public DateTime LastActivityAt { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("deviceInfo")]
    public string DeviceInfo { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

public class UserSessionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("loginAt")]
    public DateTime LoginAt { get; set; }

    [JsonPropertyName("lastActivityAt")]
    public DateTime? LastActivityAt { get; set; }

    [JsonPropertyName("logoutAt")]
    public DateTime? LogoutAt { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("deviceInfo")]
    public string DeviceInfo { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class SessionStatsDto
{
    [JsonPropertyName("totalActiveSessions")]
    public int TotalActiveSessions { get; set; }

    [JsonPropertyName("totalSessionsToday")]
    public int TotalSessionsToday { get; set; }

    [JsonPropertyName("totalSessionsThisWeek")]
    public int TotalSessionsThisWeek { get; set; }

    [JsonPropertyName("totalSessionsThisMonth")]
    public int TotalSessionsThisMonth { get; set; }

    [JsonPropertyName("averageSessionDuration")]
    public TimeSpan AverageSessionDuration { get; set; }

    [JsonPropertyName("longestActiveSession")]
    public TimeSpan LongestActiveSession { get; set; }

    [JsonPropertyName("sessionsByDepartment")]
    public Dictionary<string, int> SessionsByDepartment { get; set; } = new();

    [JsonPropertyName("sessionsByHour")]
    public Dictionary<int, int> SessionsByHour { get; set; } = new();

    [JsonPropertyName("uniqueUsersToday")]
    public int UniqueUsersToday { get; set; }

    [JsonPropertyName("uniqueUsersThisWeek")]
    public int UniqueUsersThisWeek { get; set; }

    [JsonPropertyName("uniqueUsersThisMonth")]
    public int UniqueUsersThisMonth { get; set; }
}

public class LoginHistoryDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("loginAt")]
    public DateTime LoginAt { get; set; }

    [JsonPropertyName("logoutAt")]
    public DateTime? LogoutAt { get; set; }

    [JsonPropertyName("isSuccessful")]
    public bool IsSuccessful { get; set; }

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("deviceInfo")]
    public string DeviceInfo { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("sessionDuration")]
    public TimeSpan? SessionDuration { get; set; }
}

public class ResetPasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [JsonPropertyName("sendEmailNotification")]
    public bool SendEmailNotification { get; set; } = true;
}

public class TerminateSessionRequest
{
    [Required]
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class SessionFilterRequest
{
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}
