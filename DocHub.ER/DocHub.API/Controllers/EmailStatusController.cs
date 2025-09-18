using Microsoft.AspNetCore.Mvc;
using DocHub.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailStatusController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailStatusController> _logger;

    public EmailStatusController(IEmailService emailService, ILogger<EmailStatusController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("poll")]
    public async Task<IActionResult> TriggerEmailStatusPolling()
    {
        try
        {
            _logger.LogInformation("🔄 [API] Manual email status polling triggered at {Timestamp}", DateTime.UtcNow);
            _logger.LogDebug("📧 [API] Calling EmailService.PollEmailStatusesAsync");
            
            await _emailService.PollEmailStatusesAsync();
            
            _logger.LogInformation("✅ [API] Manual email status polling completed successfully at {Timestamp}", DateTime.UtcNow);
            return Ok(new { message = "Email status polling completed successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [API] Error during manual email status polling: {Message}", ex.Message);
            return StatusCode(500, new { message = "Error during email status polling", error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpGet("test")]
    public IActionResult TestNotification()
    {
        _logger.LogDebug("🧪 [API] Email status polling test endpoint called at {Timestamp}", DateTime.UtcNow);
        
        var response = new { 
            message = "Email status polling system is running",
            pollingInterval = "1 minute",
            status = "active",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        };
        
        _logger.LogInformation("✅ [API] Email status polling test response sent: {Response}", response);
        return Ok(response);
    }
}
