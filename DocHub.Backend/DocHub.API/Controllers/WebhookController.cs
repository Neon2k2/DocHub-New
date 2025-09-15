using Microsoft.AspNetCore.Mvc;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Emails;
using System.Text.Json;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IEmailService emailService, ILogger<WebhookController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("sendgrid")]
    public async Task<IActionResult> SendGridWebhook([FromBody] JsonElement payload)
    {
        try
        {
            _logger.LogInformation("Received SendGrid webhook: {Payload}", payload.GetRawText());

            // Parse the webhook event
            var webhookEvent = new DocHub.Core.Entities.WebhookEvent
            {
                EventType = payload.GetProperty("event").GetString() ?? "unknown",
                Source = "SendGrid",
                Payload = payload.GetRawText(),
                CreatedAt = DateTime.UtcNow
            };

            // Process the webhook
            await _emailService.ProcessSendGridWebhookAsync(webhookEvent);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendGrid webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("email-status")]
    public async Task<IActionResult> UpdateEmailStatus([FromBody] EmailStatusUpdate statusUpdate)
    {
        try
        {
            _logger.LogInformation("Updating email status: {EmailJobId} - {Status}", 
                statusUpdate.EmailJobId, statusUpdate.Status);

            await _emailService.UpdateEmailStatusAsync(statusUpdate);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email status");
            return StatusCode(500, "Internal server error");
        }
    }
}

