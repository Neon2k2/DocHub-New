using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{module}/stats")]
    public Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats(string module)
    {
        try
        {
            // For now, return mock data - this can be implemented with real statistics later
            var stats = new DashboardStatsDto
            {
                TotalEmployees = 0,
                ActiveEmployees = 0,
                NewJoiningsThisMonth = 0,
                RelievedThisMonth = 0,
                TotalProjects = 0,
                ActiveProjects = 0,
                TotalHoursThisMonth = 0,
                BillableHours = 0,
                PendingTimesheets = 0,
                ApprovedTimesheets = 0,
                TotalRevenue = 0,
                RecentActivities = new List<ActivityDto>()
            };

            return Task.FromResult<ActionResult<ApiResponse<DashboardStatsDto>>>(Ok(ApiResponse<DashboardStatsDto>.SuccessResult(stats)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats for module {Module}", module);
            return Task.FromResult<ActionResult<ApiResponse<DashboardStatsDto>>>(StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResult("An error occurred while getting dashboard stats")));
        }
    }
}

public class DashboardStatsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int NewJoiningsThisMonth { get; set; }
    public int RelievedThisMonth { get; set; }
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public decimal TotalHoursThisMonth { get; set; }
    public decimal BillableHours { get; set; }
    public int PendingTimesheets { get; set; }
    public int ApprovedTimesheets { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<ActivityDto> RecentActivities { get; set; } = new();
}

public class ActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
