using DocHub.API.DTOs;
using DocHub.API.Models;

namespace DocHub.API.Services.Interfaces;

public interface IWebhookService
{
    Task ProcessSendGridWebhookAsync(SendGridWebhookEvent webhookEvent);
    Task ProcessEmailStatusUpdateAsync(string emailJobId, string status, string? reason = null);
    Task<List<WebhookEvent>> GetWebhookEventsAsync(string? emailJobId = null, DateTime? startDate = null, DateTime? endDate = null);
    bool ValidateWebhookSignature(string payload, string signature, string secret);
}