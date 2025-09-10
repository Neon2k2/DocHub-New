using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface ITemplateService
{
    Task<List<DocumentTemplateSummary>> GetTemplatesAsync(string? type = null, bool? isActive = null);
    Task<DocumentTemplateDetail> GetTemplateAsync(Guid templateId);
    Task<DocumentTemplateSummary> CreateTemplateAsync(CreateDocumentTemplateRequest request);
    Task<DocumentTemplateSummary> UpdateTemplateAsync(Guid templateId, UpdateDocumentTemplateRequest request);
    Task DeleteTemplateAsync(Guid templateId);
    Task<DocumentTemplateSummary> UploadTemplateAsync(Guid templateId, IFormFile file);
    Task<byte[]> DownloadTemplateAsync(Guid templateId);
    Task<List<string>> GetPlaceholdersAsync(Guid templateId);
    Task<TemplateValidationResult> ValidateTemplateAsync(Guid templateId, TemplateValidationRequest request);
}
