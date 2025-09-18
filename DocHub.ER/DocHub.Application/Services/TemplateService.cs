using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DocHub.Infrastructure.Data;

namespace DocHub.Application.Services;

public class TemplateService : ITemplateService
{
    private readonly IRepository<DocumentTemplate> _templateRepository;
    private readonly IDbContext _dbContext;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(IRepository<DocumentTemplate> templateRepository, IDbContext dbContext, ILogger<TemplateService> logger)
    {
        _templateRepository = templateRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DocumentTemplate?> GetTemplateByIdAsync(string templateId)
    {
        try
        {
            return await _templateRepository.FirstOrDefaultAsync(t => t.Id == Guid.Parse(templateId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template: {TemplateId}", templateId);
            return null;
        }
    }

    public async Task<List<DocumentTemplate>> GetTemplatesAsync()
    {
        try
        {
            var templates = await _templateRepository.GetAllAsync();
            return templates.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return new List<DocumentTemplate>();
        }
    }

    public async Task<DocumentTemplate> CreateTemplateAsync(string name, byte[] content, string? filePath = null)
    {
        try
        {
            // Create a file reference first
            var fileReference = new FileReference
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(filePath ?? "template.docx"),
                FilePath = filePath ?? "",
                FileSize = content.Length,
                MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                Category = "template",
                UploadedBy = Guid.Empty, // This should be passed as parameter
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };

            await ((DocHubDbContext)_dbContext).FileReferences.AddAsync(fileReference);
            await _dbContext.SaveChangesAsync();

            var template = new DocumentTemplate
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = "letter_template",
                FileId = fileReference.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(template);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Template created successfully: {TemplateId}", template.Id);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template: {TemplateName}", name);
            throw;
        }
    }
}
