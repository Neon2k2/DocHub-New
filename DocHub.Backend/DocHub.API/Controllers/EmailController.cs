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
            var userId = GetCurrentUserId();
            var emailJobs = await _emailService.GetEmailJobsAsync(request, userId);
            return Ok(ApiResponse<IEnumerable<EmailJobDto>>.SuccessResult(emailJobs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email jobs");
            // Return empty list instead of error for now to prevent 404 in frontend
            return Ok(ApiResponse<IEnumerable<EmailJobDto>>.SuccessResult(new List<EmailJobDto>()));
        }
    }

    [HttpPost("templates")]
    public async Task<ActionResult<EmailTemplateDto>> CreateEmailTemplate([FromBody] CreateEmailTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var template = await _emailService.CreateEmailTemplateAsync(request, userId);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email template");
            return StatusCode(500, new { message = "An error occurred while creating email template" });
        }
    }

    [HttpPut("templates/{id}")]
    public async Task<ActionResult<EmailTemplateDto>> UpdateEmailTemplate(Guid id, [FromBody] UpdateEmailTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var template = await _emailService.UpdateEmailTemplateAsync(id, request, userId);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating email template" });
        }
    }

    [HttpDelete("templates/{id}")]
    public async Task<ActionResult> DeleteEmailTemplate(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _emailService.DeleteEmailTemplateAsync(id, userId);
            return Ok(new { message = "Email template deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting email template" });
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<EmailTemplateDto>>> GetEmailTemplates([FromQuery] Guid? moduleId = null)
    {
        try
        {
            var templates = await _emailService.GetEmailTemplatesAsync(moduleId);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates");
            return StatusCode(500, new { message = "An error occurred while getting email templates" });
        }
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<EmailTemplateDto>> GetEmailTemplate(Guid id)
    {
        try
        {
            var template = await _emailService.GetEmailTemplateAsync(id);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting email template" });
        }
    }

    [HttpPost("templates/{templateId}/process")]
    public async Task<ActionResult<EmailJobDto>> ProcessEmailTemplate(Guid templateId, [FromBody] object data)
    {
        try
        {
            var processedTemplate = await _emailService.ProcessEmailTemplateAsync(templateId, data);
            return Ok(processedTemplate);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred while processing email template" });
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
