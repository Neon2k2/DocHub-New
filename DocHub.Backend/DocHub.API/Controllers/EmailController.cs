using Microsoft.AspNetCore.Mvc;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IEmailService emailService,
        ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Send bulk emails to multiple employees
    /// </summary>
    [HttpPost("send-bulk")]
    public async Task<ActionResult<ApiResponse<BulkEmailResult>>> SendBulk([FromBody] BulkEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<BulkEmailResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request data",
                        Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var result = await _emailService.SendBulkAsync(request);

            return Ok(new ApiResponse<BulkEmailResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk emails");
            return StatusCode(500, new ApiResponse<BulkEmailResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to send bulk emails"
                }
            });
        }
    }

    /// <summary>
    /// Send email to a single employee
    /// </summary>
    [HttpPost("send-single")]
    public async Task<ActionResult<ApiResponse<EmailResult>>> SendSingle([FromBody] SingleEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<EmailResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                    Message = "Invalid request data",
                    Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var result = await _emailService.SendSingleAsync(request);

            return Ok(new ApiResponse<EmailResult>
            {
                Success = result.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send single email");
            return StatusCode(500, new ApiResponse<EmailResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to send email"
                }
            });
        }
    }

    /// <summary>
    /// Get email job status
    /// </summary>
    [HttpGet("job/{jobId}/status")]
    public async Task<ActionResult<ApiResponse<EmailJobStatus>>> GetJobStatus(Guid jobId)
    {
        try
        {
            var status = await _emailService.GetJobStatusAsync(jobId);

            return Ok(new ApiResponse<EmailJobStatus>
            {
                Success = true,
                Data = status
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<EmailJobStatus>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email job status for {JobId}", jobId);
            return StatusCode(500, new ApiResponse<EmailJobStatus>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get job status"
                }
            });
        }
    }

    /// <summary>
    /// Get all email jobs for a user
    /// </summary>
    [HttpGet("jobs")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmailJobSummary>>>> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var jobs = await _emailService.GetJobsAsync(page, pageSize, status, search);

            return Ok(new ApiResponse<PagedResult<EmailJobSummary>>
            {
                Success = true,
                Data = jobs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email jobs");
            return StatusCode(500, new ApiResponse<PagedResult<EmailJobSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get email jobs"
                }
            });
        }
    }

    /// <summary>
    /// Cancel an email job
    /// </summary>
    [HttpPost("job/{jobId}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> CancelJob(Guid jobId)
    {
        try
        {
            await _emailService.CancelJobAsync(jobId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Job cancelled successfully" }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INVALID_OPERATION",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel email job {JobId}", jobId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to cancel job"
                }
            });
        }
    }

    /// <summary>
    /// Retry a failed email job
    /// </summary>
    [HttpPost("job/{jobId}/retry")]
    public async Task<ActionResult<ApiResponse<object>>> RetryJob(Guid jobId)
    {
        try
        {
            await _emailService.RetryJobAsync(jobId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Job retry initiated successfully" }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INVALID_OPERATION",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry email job {JobId}", jobId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to retry job"
                }
            });
        }
    }

    /// <summary>
    /// Get email templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<List<EmailTemplateSummary>>>> GetTemplates()
    {
        try
        {
            var templates = await _emailService.GetTemplatesAsync();

            return Ok(new ApiResponse<List<EmailTemplateSummary>>
            {
                Success = true,
                Data = templates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email templates");
            return StatusCode(500, new ApiResponse<List<EmailTemplateSummary>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to get email templates"
                }
            });
        }
    }

    /// <summary>
    /// Create email template
    /// </summary>
    [HttpPost("templates")]
    public async Task<ActionResult<ApiResponse<EmailTemplateSummary>>> CreateTemplate([FromBody] CreateEmailTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<EmailTemplateSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request data",
                        Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var template = await _emailService.CreateTemplateAsync(request);

            return Ok(new ApiResponse<EmailTemplateSummary>
            {
                Success = true,
                Data = template
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create email template");
            return StatusCode(500, new ApiResponse<EmailTemplateSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create email template"
                }
            });
        }
    }

    /// <summary>
    /// Update email template
    /// </summary>
    [HttpPut("templates/{templateId}")]
    public async Task<ActionResult<ApiResponse<EmailTemplateSummary>>> UpdateTemplate(
        Guid templateId,
        [FromBody] UpdateEmailTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<EmailTemplateSummary>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request data",
                        Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    }
                });
            }

            var template = await _emailService.UpdateTemplateAsync(templateId, request);

            return Ok(new ApiResponse<EmailTemplateSummary>
            {
                Success = true,
                Data = template
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<EmailTemplateSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update email template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<EmailTemplateSummary>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update email template"
                }
            });
        }
    }

    /// <summary>
    /// Delete email template
    /// </summary>
    [HttpDelete("templates/{templateId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTemplate(Guid templateId)
    {
        try
        {
            await _emailService.DeleteTemplateAsync(templateId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Template deleted successfully" }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete email template {TemplateId}", templateId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete email template"
                }
            });
        }
    }
}
