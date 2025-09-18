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
