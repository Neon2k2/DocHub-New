using Microsoft.AspNetCore.Mvc;
using DocHub.Application.Services;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsightsController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(
        IEmailService emailService, 
        ICacheService cacheService,
        ILogger<InsightsController> logger)
    {
        _emailService = emailService;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("{tabId}")]
    public async Task<IActionResult> GetInsights(
        string tabId,
        [FromQuery] string timeRange = "30d")
    {
        try
        {
            _logger.LogInformation("Getting insights for tab: {TabId}, timeRange: {TimeRange}", 
                tabId, timeRange);

            var cacheKey = $"insights:{tabId}:{timeRange}";
            
            var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var insights = await _emailService.GetInsightsAsync(tabId, timeRange);
                return insights;
            }, TimeSpan.FromMinutes(10));

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights for tab: {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError { Message = "Failed to get insights" }
            });
        }
    }

    [HttpGet("{tabId}/analytics")]
    public async Task<IActionResult> GetAnalytics(
        string tabId,
        [FromQuery] string timeRange = "30d",
        [FromQuery] string metric = "all")
    {
        try
        {
            _logger.LogInformation("Getting analytics for tab: {TabId}, timeRange: {TimeRange}, metric: {Metric}", 
                tabId, timeRange, metric);

            var cacheKey = $"analytics:{tabId}:{timeRange}:{metric}";
            
            var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var analytics = await _emailService.GetAnalyticsAsync(tabId, timeRange, metric);
                return analytics;
            }, TimeSpan.FromMinutes(15));

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for tab: {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError { Message = "Failed to get analytics" }
            });
        }
    }
}
