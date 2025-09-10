using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface IEmailService
{
    Task<BulkEmailResult> SendBulkAsync(BulkEmailRequest request);
    Task<EmailResult> SendSingleAsync(SingleEmailRequest request);
    Task<EmailJobStatus> GetJobStatusAsync(Guid jobId);
    Task<PagedResult<EmailJobSummary>> GetJobsAsync(int page, int pageSize, string? status = null, string? search = null);
    Task CancelJobAsync(Guid jobId);
    Task RetryJobAsync(Guid jobId);
    Task<List<EmailTemplateSummary>> GetTemplatesAsync();
    Task<EmailTemplateSummary> CreateTemplateAsync(CreateEmailTemplateRequest request);
    Task<EmailTemplateSummary> UpdateTemplateAsync(Guid templateId, UpdateEmailTemplateRequest request);
    Task DeleteTemplateAsync(Guid templateId);
}