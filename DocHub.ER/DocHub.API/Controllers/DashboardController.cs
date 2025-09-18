using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Dashboard;
using DocHub.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;
    private readonly IDbContext _dbContext;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger, IDbContext dbContext)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _dbContext = dbContext;
    }

        [HttpGet("{module}/stats")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats(string module)
        {
            try
            {
                _logger.LogInformation("📊 [DASHBOARD-CONTROLLER] Getting dashboard stats for module: {Module}", module);
                
                // Get current user context
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");
                
                _logger.LogInformation("👤 [DASHBOARD-CONTROLLER] User context - UserId: {UserId}, IsAdmin: {IsAdmin}", userId, isAdmin);
                
                // Add timeout to prevent hanging requests
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var stats = await _dashboardService.GetDashboardStatsAsync(module, userId, isAdmin);
                
                _logger.LogInformation("✅ [DASHBOARD-CONTROLLER] Successfully retrieved dashboard stats for module: {Module} - TotalUsers: {TotalUsers}, ActiveUsers: {ActiveUsers}", 
                    module, stats.TotalUsers, stats.ActiveUsers);
                return Ok(ApiResponse<DashboardStatsDto>.SuccessResult(stats));
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "⏰ [DASHBOARD-CONTROLLER] Request timeout getting dashboard stats for module {Module}", module);
                return StatusCode(408, ApiResponse<DashboardStatsDto>.ErrorResult("Request timeout - please try again"));
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "⏰ [DASHBOARD-CONTROLLER] Request timeout getting dashboard stats for module {Module}", module);
                return StatusCode(408, ApiResponse<DashboardStatsDto>.ErrorResult("Request timeout - please try again"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DASHBOARD-CONTROLLER] Error getting dashboard stats for module {Module}", module);
                return StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResult("An error occurred while getting dashboard stats"));
            }
        }

        [HttpGet("debug-data")]
        public async Task<ActionResult> GetDebugData()
        {
            try
            {
                var emailJobs = await _dbContext.EmailJobs
                    .Select(ej => new { ej.Id, ej.Status, ej.CreatedAt, ej.RecipientEmail })
                    .Take(10)
                    .ToListAsync();
                
                var letterTypes = await _dbContext.LetterTypeDefinitions
                    .Select(lt => new { lt.Id, lt.DisplayName, lt.IsActive })
                    .ToListAsync();
                
                var documents = await _dbContext.GeneratedDocuments
                    .Select(gd => new { gd.Id, gd.GeneratedAt })
                    .Take(10)
                    .ToListAsync();
                
                var totalEmailJobs = await _dbContext.EmailJobs.CountAsync();
                var totalLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync();
                var totalDocuments = await _dbContext.GeneratedDocuments.CountAsync();
                
                return Ok(new { 
                    EmailJobs = emailJobs,
                    LetterTypes = letterTypes,
                    Documents = documents,
                    EmailJobCount = totalEmailJobs,
                    LetterTypeCount = totalLetterTypes,
                    DocumentCount = totalDocuments,
                    Message = "Direct database counts"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("document-requests")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<DocumentRequestDto>>>> GetDocumentRequests(
        [FromQuery] string? documentType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? employeeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting document requests with filters: DocumentType={DocumentType}, Status={Status}, EmployeeId={EmployeeId}", 
                documentType, status, employeeId);

            var request = new GetDocumentRequestsRequest
            {
                DocumentType = documentType,
                Status = status,
                EmployeeId = employeeId,
                Page = page,
                Limit = limit
            };

            var documentRequests = await _dashboardService.GetDocumentRequestsAsync(request);
            var totalCount = await GetDocumentRequestsCountAsync(request);

            var response = new PaginatedResponse<DocumentRequestDto>
            {
                Items = documentRequests,
                TotalCount = totalCount,
                Page = page,
                PageSize = limit,
                TotalPages = (int)Math.Ceiling((double)totalCount / limit)
            };

            _logger.LogInformation("Successfully retrieved {Count} document requests", documentRequests.Count());
            return Ok(ApiResponse<PaginatedResponse<DocumentRequestDto>>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document requests");
            return StatusCode(500, ApiResponse<PaginatedResponse<DocumentRequestDto>>.ErrorResult("An error occurred while getting document requests"));
        }
    }

    [HttpGet("document-requests/stats")]
    public async Task<ActionResult<ApiResponse<DocumentRequestStatsDto>>> GetDocumentRequestStats()
    {
        try
        {
            _logger.LogInformation("Getting document request statistics");

            var stats = await _dashboardService.GetDocumentRequestStatsAsync();

            _logger.LogInformation("Successfully retrieved document request statistics");
            return Ok(ApiResponse<DocumentRequestStatsDto>.SuccessResult(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document request statistics");
            return StatusCode(500, ApiResponse<DocumentRequestStatsDto>.ErrorResult("An error occurred while getting document request statistics"));
        }
    }

    private async Task<int> GetDocumentRequestsCountAsync(GetDocumentRequestsRequest request)
    {
        // This is a simplified implementation - in a real scenario, you'd want to optimize this
        var allRequests = await _dashboardService.GetDocumentRequestsAsync(new GetDocumentRequestsRequest
        {
            DocumentType = request.DocumentType,
            Status = request.Status,
            EmployeeId = request.EmployeeId,
            Page = 1,
            Limit = int.MaxValue
        });
        
        return allRequests.Count();
    }
}
