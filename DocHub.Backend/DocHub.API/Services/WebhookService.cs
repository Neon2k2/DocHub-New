using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DocHub.API.Services;

public class WebhookService : IWebhookService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(DocHubDbContext context, ILogger<WebhookService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessSendGridWebhookAsync(SendGridWebhookEvent webhookEvent)
    {
        try
        {
            // Find the email job by SendGrid message ID
            var emailJob = await _context.EmailJobs
                .FirstOrDefaultAsync(ej => ej.SendGridMessageId == webhookEvent.SgMessageId);

            if (emailJob == null)
            {
                _logger.LogWarning("Email job not found for SendGrid message ID: {MessageId}", webhookEvent.SgMessageId);
                return;
            }

            // Update email job status based on webhook event
            switch (webhookEvent.Event.ToLowerInvariant())
            {
                case "delivered":
                    emailJob.Status = "Delivered";
                    emailJob.DeliveredAt = webhookEvent.Timestamp;
                    break;
                case "bounce":
                    emailJob.Status = "Bounced";
                    emailJob.BouncedAt = webhookEvent.Timestamp;
                    emailJob.BounceReason = webhookEvent.Reason;
                    break;
                case "dropped":
                    emailJob.Status = "Dropped";
                    emailJob.DroppedAt = webhookEvent.Timestamp;
                    emailJob.DropReason = webhookEvent.Reason;
                    break;
                case "spam_report":
                    emailJob.Status = "SpamReported";
                    emailJob.SpamReportedAt = webhookEvent.Timestamp;
                    break;
                case "unsubscribe":
                    emailJob.Status = "Unsubscribed";
                    emailJob.UnsubscribedAt = webhookEvent.Timestamp;
                    break;
                case "open":
                    emailJob.Status = "Opened";
                    emailJob.OpenedAt = webhookEvent.Timestamp;
                    break;
                case "click":
                    emailJob.Status = "Clicked";
                    emailJob.ClickedAt = webhookEvent.Timestamp;
                    break;
            }

            // Create webhook event record
            var webhookEventRecord = new WebhookEvent
            {
                Id = Guid.NewGuid(),
                EmailJobId = emailJob.Id.ToString(),
                EventType = webhookEvent.Event,
                Status = webhookEvent.Status,
                Reason = webhookEvent.Reason,
                Response = webhookEvent.Response,
                Attempt = webhookEvent.Attempt,
                SgEventId = webhookEvent.SgEventId,
                SgMessageId = webhookEvent.SgMessageId,
                Timestamp = webhookEvent.Timestamp,
                Category = webhookEvent.Category,
                UniqueArgs = webhookEvent.UniqueArgs,
                Url = webhookEvent.Url,
                UserAgent = webhookEvent.UserAgent,
                Ip = webhookEvent.Ip,
                ProcessedAt = DateTime.UtcNow
            };

            _context.WebhookEvents.Add(webhookEventRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed SendGrid webhook event: {Event} for email job {EmailJobId}", 
                webhookEvent.Event, emailJob.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendGrid webhook event: {Event}", webhookEvent.Event);
            throw;
        }
    }

    public async Task ProcessEmailStatusUpdateAsync(string emailJobId, string status, string? reason = null)
    {
        try
        {
            var emailJob = await _context.EmailJobs
                .FirstOrDefaultAsync(ej => ej.Id.ToString() == emailJobId);

            if (emailJob == null)
            {
                _logger.LogWarning("Email job not found: {EmailJobId}", emailJobId);
                return;
            }

            emailJob.Status = status;
            emailJob.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(reason))
            {
                if (status == "Bounced")
                    emailJob.BounceReason = reason;
                else if (status == "Dropped")
                    emailJob.DropReason = reason;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated email job {EmailJobId} status to {Status}", emailJobId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email job status: {EmailJobId}", emailJobId);
            throw;
        }
    }

    public async Task<List<WebhookEvent>> GetWebhookEventsAsync(string? emailJobId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.WebhookEvents.AsQueryable();

            if (!string.IsNullOrEmpty(emailJobId))
            {
                query = query.Where(we => we.EmailJobId == emailJobId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(we => we.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(we => we.Timestamp <= endDate.Value);
            }

            return await query
                .OrderByDescending(we => we.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook events");
            throw;
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        try
        {
            var expectedSignature = ComputeHmacSha256(payload, secret);
            return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
