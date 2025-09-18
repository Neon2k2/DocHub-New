using Microsoft.AspNetCore.Mvc;
using DocHub.Application.Services;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Emails;
using Microsoft.AspNetCore.Authorization;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailHistoryController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<EmailHistoryController> _logger;

    public EmailHistoryController(
        IEmailService emailService, 
        ICacheService cacheService,
        ILogger<EmailHistoryController> logger)
    {
        _emailService = emailService;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("{tabId}")]
    public async Task<IActionResult> GetEmailHistory(
        string tabId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        try
        {
            _logger.LogInformation("Getting email history for tab: {TabId}, page: {Page}, limit: {Limit}", 
                tabId, page, limit);

            var cacheKey = $"email_history:{tabId}:{page}:{limit}:{status}:{search}";
            
            var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var emails = await _emailService.GetEmailHistoryAsync(tabId, new GetEmailHistoryRequest
                {
                    Page = page,
                    PageSize = limit,
                    Status = status,
                    SearchTerm = search
                });

                return new
                {
                    items = emails.Items,
                    total = emails.TotalCount,
                    page = emails.Page,
                    pageSize = emails.PageSize,
                    totalPages = emails.TotalPages
                };
            }, TimeSpan.FromMinutes(5));

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email history for tab: {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError { Message = "Failed to get email history" }
            });
        }
    }

    [HttpGet("{tabId}/stats")]
    public async Task<IActionResult> GetEmailStats(string tabId)
    {
        try
        {
            _logger.LogInformation("Getting email stats for tab: {TabId}", tabId);

            var cacheKey = $"email_stats:{tabId}";
            
            var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var stats = await _emailService.GetEmailStatsAsync(tabId);
                return stats;
            }, TimeSpan.FromMinutes(10));

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email stats for tab: {TabId}", tabId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError { Message = "Failed to get email stats" }
            });
        }
    }
}
