using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Dashboard;

public class DashboardStatsDto
{
    [JsonPropertyName("totalUsers")]
    public int TotalUsers { get; set; }

    [JsonPropertyName("activeUsers")]
    public int ActiveUsers { get; set; }

    [JsonPropertyName("systemUptime")]
    public decimal SystemUptime { get; set; }

    [JsonPropertyName("activeSessions")]
    public int ActiveSessions { get; set; }

    [JsonPropertyName("newJoiningsThisMonth")]
    public int NewJoiningsThisMonth { get; set; }

    [JsonPropertyName("relievedThisMonth")]
    public int RelievedThisMonth { get; set; }

    [JsonPropertyName("totalProjects")]
    public int TotalProjects { get; set; }

    [JsonPropertyName("activeProjects")]
    public int ActiveProjects { get; set; }

    [JsonPropertyName("totalHoursThisMonth")]
    public decimal TotalHoursThisMonth { get; set; }

    [JsonPropertyName("billableHours")]
    public decimal BillableHours { get; set; }

    [JsonPropertyName("pendingTimesheets")]
    public int PendingTimesheets { get; set; }

    [JsonPropertyName("approvedTimesheets")]
    public int ApprovedTimesheets { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("recentActivities")]
    public List<ActivityDto> RecentActivities { get; set; } = new();
}

public class ActivityDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = string.Empty;

    [JsonPropertyName("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class DocumentRequestDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = string.Empty;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("requestedBy")]
    public string RequestedBy { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("processedAt")]
    public DateTime? ProcessedAt { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}

public class DocumentRequestStatsDto
{
    [JsonPropertyName("totalRequests")]
    public int TotalRequests { get; set; }

    [JsonPropertyName("pendingRequests")]
    public int PendingRequests { get; set; }

    [JsonPropertyName("approvedRequests")]
    public int ApprovedRequests { get; set; }

    [JsonPropertyName("rejectedRequests")]
    public int RejectedRequests { get; set; }

    [JsonPropertyName("inProgressRequests")]
    public int InProgressRequests { get; set; }
}

public class GetDocumentRequestsRequest
{
    [JsonPropertyName("documentType")]
    public string? DocumentType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}
