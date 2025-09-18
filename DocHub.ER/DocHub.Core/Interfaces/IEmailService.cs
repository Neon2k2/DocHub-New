using DocHub.Core.Entities;
using DocHub.Shared.DTOs.Emails;
using DocHub.Shared.DTOs.Common;

namespace DocHub.Core.Interfaces;

public interface IEmailService
{
    // Email Sending
    Task<EmailJobDto> SendEmailAsync(SendEmailRequest request, string userId);
    Task<IEnumerable<EmailJobDto>> SendBulkEmailsAsync(SendBulkEmailsRequest request, string userId);
    Task<EmailJobDto> GetEmailStatusAsync(Guid jobId);
    Task<IEnumerable<EmailJobDto>> GetEmailJobsAsync(GetEmailJobsRequest request, string userId);

    // Email Processing
    Task<string> RenderEmailContentAsync(string template, object data);
    Task<bool> ValidateEmailAddressAsync(string email);

    // Webhook Processing
    Task ProcessSendGridWebhookAsync(WebhookEvent webhookEvent);
    Task UpdateEmailStatusAsync(EmailStatusUpdate statusUpdate);
    Task ProcessEmailEventAsync(string eventType, object payload);

    // Email Analytics
    Task<Dictionary<string, object>> GetEmailAnalyticsAsync(Guid? letterTypeId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<EmailJobDto>> GetFailedEmailsAsync(int page = 1, int pageSize = 20);
    Task<bool> RetryFailedEmailAsync(Guid jobId, string userId);

    // Email Status Polling
    Task PollEmailStatusesAsync();

    // Email History
    Task<PaginatedResponse<EmailJobDto>> GetEmailHistoryAsync(string tabId, GetEmailHistoryRequest request);
    Task<EmailStatsDto> GetEmailStatsAsync(string tabId);

    // Insights and Analytics
    Task<object> GetInsightsAsync(string tabId, string timeRange);
    Task<object> GetAnalyticsAsync(string tabId, string timeRange, string metric);
}
