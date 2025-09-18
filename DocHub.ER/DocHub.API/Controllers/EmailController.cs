using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Emails;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<ActionResult<EmailJobDto>> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var emailJob = await _emailService.SendEmailAsync(request, userId);
            return Ok(emailJob);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return StatusCode(500, new { message = "An error occurred while sending email" });
        }
    }

    [HttpPost("send-bulk")]
    public async Task<ActionResult<IEnumerable<EmailJobDto>>> SendBulkEmails([FromBody] SendBulkEmailsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var emailJobs = await _emailService.SendBulkEmailsAsync(request, userId);
            return Ok(emailJobs);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk emails");
            return StatusCode(500, new { message = "An error occurred while sending bulk emails" });
        }
    }

    [HttpGet("{jobId}/status")]
    public async Task<ActionResult<EmailJobDto>> GetEmailStatus(Guid jobId)
    {
        try
        {
            var emailJob = await _emailService.GetEmailStatusAsync(jobId);
            return Ok(emailJob);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email status for job {JobId}", jobId);
            return StatusCode(500, new { message = "An error occurred while getting email status" });
        }
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailJobDto>>>> GetEmailJobs([FromQuery] GetEmailJobsRequest request)
    {
        try
        {
            _logger.LogInformation("📧 [EMAIL-CONTROLLER] Getting email jobs with page {Page}, pageSize {PageSize}", request.Page, request.PageSize);
            
            // Validate and limit page size
            if (request.PageSize <= 0 || request.PageSize > 100)
            {
                request.PageSize = 20; // Default page size
            }
            
            if (request.Page <= 0)
            {
                request.Page = 1;
            }

            var userId = GetCurrentUserId();
            var emailJobs = await _emailService.GetEmailJobsAsync(request, userId);
            
            _logger.LogInformation("✅ [EMAIL-CONTROLLER] Successfully retrieved {Count} email jobs", emailJobs.Count());
            return Ok(ApiResponse<IEnumerable<EmailJobDto>>.SuccessResult(emailJobs));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "⏰ [EMAIL-CONTROLLER] Request timeout getting email jobs");
            return Ok(ApiResponse<IEnumerable<EmailJobDto>>.SuccessResult(new List<EmailJobDto>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL-CONTROLLER] Error getting email jobs");
            // Return empty list instead of error for now to prevent 404 in frontend
            return Ok(ApiResponse<IEnumerable<EmailJobDto>>.SuccessResult(new List<EmailJobDto>()));
        }
    }




    [HttpPost("validate-email")]
    public async Task<ActionResult<bool>> ValidateEmailAddress([FromBody] ValidateEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _emailService.ValidateEmailAddressAsync(request.Email);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email address");
            return StatusCode(500, new { message = "An error occurred while validating email address" });
        }
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<Dictionary<string, object>>> GetEmailAnalytics([FromQuery] Guid? letterTypeId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var analytics = await _emailService.GetEmailAnalyticsAsync(letterTypeId, fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email analytics");
            return StatusCode(500, new { message = "An error occurred while getting email analytics" });
        }
    }

    [HttpGet("failed")]
    public async Task<ActionResult<IEnumerable<EmailJobDto>>> GetFailedEmails([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var failedEmails = await _emailService.GetFailedEmailsAsync(page, pageSize);
            return Ok(failedEmails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed emails");
            return StatusCode(500, new { message = "An error occurred while getting failed emails" });
        }
    }

    [HttpPost("{jobId}/retry")]
    public async Task<ActionResult> RetryFailedEmail(Guid jobId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _emailService.RetryFailedEmailAsync(jobId, userId);
            if (!success)
            {
                return BadRequest(new { message = "Failed to retry email" });
            }

            return Ok(new { message = "Email retry initiated successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed email {JobId}", jobId);
            return StatusCode(500, new { message = "An error occurred while retrying email" });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("nameid")?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found in token");
    }
}
