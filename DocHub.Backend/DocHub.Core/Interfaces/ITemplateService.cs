using DocHub.Core.Entities;

namespace DocHub.Core.Interfaces;

public interface ITemplateService
{
    Task<DocumentTemplate?> GetTemplateByIdAsync(string templateId);
    Task<List<DocumentTemplate>> GetTemplatesAsync();
    Task<DocumentTemplate> CreateTemplateAsync(string name, byte[] content, string? filePath = null);
}
