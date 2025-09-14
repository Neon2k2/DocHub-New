using DocHub.Core.Entities;
using DocHub.Shared.DTOs.Emails;

namespace DocHub.Core.Interfaces;

public interface IEmailService
{
    // Email Sending
    Task<EmailJobDto> SendEmailAsync(SendEmailRequest request, string userId);
    Task<IEnumerable<EmailJobDto>> SendBulkEmailsAsync(SendBulkEmailsRequest request, string userId);
    Task<EmailJobDto> GetEmailStatusAsync(Guid jobId);
    Task<IEnumerable<EmailJobDto>> GetEmailJobsAsync(GetEmailJobsRequest request, string userId);

    // Email Template Management
    Task<EmailTemplateDto> CreateEmailTemplateAsync(CreateEmailTemplateRequest request, string userId);
    Task<EmailTemplateDto> UpdateEmailTemplateAsync(Guid id, UpdateEmailTemplateRequest request, string userId);
    Task DeleteEmailTemplateAsync(Guid id, string userId);
    Task<IEnumerable<EmailTemplateDto>> GetEmailTemplatesAsync(Guid? moduleId = null);
    Task<EmailTemplateDto> GetEmailTemplateAsync(Guid id);

    // Email Processing
    Task<EmailJobDto> ProcessEmailTemplateAsync(Guid templateId, object data);
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
}
