using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocHub.API.Services;

public class TemplateService : ITemplateService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<TemplateService> _logger;
    private readonly IFileStorageService _fileStorageService;

    public TemplateService(
        DocHubDbContext context,
        ILogger<TemplateService> logger,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    public async Task<List<DocumentTemplateSummary>> GetTemplatesAsync(string? type = null, bool? isActive = null)
    {
        var query = _context.DocumentTemplates.AsQueryable();

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query
            .OrderBy(t => t.Name)
            .Select(t => new DocumentTemplateSummary
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                FileName = t.FileName,
                IsActive = t.IsActive,
                Version = t.Version,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return templates;
    }

    public async Task<DocumentTemplateDetail> GetTemplateAsync(Guid templateId)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Document template not found");
        }

        return new DocumentTemplateDetail
        {
            Id = template.Id,
            Name = template.Name,
            Type = template.Type,
            FileName = template.FileName ?? string.Empty,
            FileUrl = template.FileUrl ?? string.Empty,
            Placeholders = template.Placeholders?.FromJson<List<string>>() ?? new List<string>(),
            IsActive = template.IsActive,
            Version = template.Version,
            CreatedBy = template.CreatedBy,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    public async Task<DocumentTemplateSummary> CreateTemplateAsync(CreateDocumentTemplateRequest request)
    {
        var template = new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            FileName = request.FileName ?? string.Empty,
            FileUrl = request.FileUrl ?? string.Empty,
            Placeholders = request.Placeholders?.ToJson(),
            IsActive = request.IsActive,
            Version = 1,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentTemplates.Add(template);
        await _context.SaveChangesAsync();

        return new DocumentTemplateSummary
        {
            Id = template.Id,
            Name = template.Name,
            Type = template.Type,
            FileName = template.FileName ?? string.Empty,
            IsActive = template.IsActive,
            Version = template.Version,
            CreatedAt = template.CreatedAt
        };
    }

    public async Task<DocumentTemplateSummary> UpdateTemplateAsync(Guid templateId, UpdateDocumentTemplateRequest request)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Document template not found");
        }

        template.Name = request.Name;
        template.Type = request.Type;
        template.FileName = request.FileName ?? string.Empty;
        template.FileUrl = request.FileUrl ?? string.Empty;
        template.Placeholders = request.Placeholders?.ToJson();
        template.IsActive = request.IsActive;
        template.Version = request.Version ?? template.Version;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentTemplateSummary
        {
            Id = template.Id,
            Name = template.Name,
            Type = template.Type,
            FileName = template.FileName ?? string.Empty,
            IsActive = template.IsActive,
            Version = template.Version,
            CreatedAt = template.CreatedAt
        };
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Document template not found");
        }

        // Delete file from storage if exists
        if (!string.IsNullOrEmpty(template.FileUrl))
        {
            // For now, skip file deletion as FileUrl is a path, not an ID
            // TODO: Implement proper file deletion using file ID
        }

        _context.DocumentTemplates.Remove(template);
        await _context.SaveChangesAsync();
    }

    public async Task<DocumentTemplateSummary> UploadTemplateAsync(Guid templateId, IFormFile file)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Document template not found");
        }

        // Store file
        var fileStorage = await _fileStorageService.StoreFileAsync(file.OpenReadStream(), file.FileName, "templates");

        // Update template
        template.FileName = file.FileName;
        template.FileUrl = fileStorage.FilePath;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentTemplateSummary
        {
            Id = template.Id,
            Name = template.Name,
            Type = template.Type,
            FileName = template.FileName ?? string.Empty,
            IsActive = template.IsActive,
            Version = template.Version,
            CreatedAt = template.CreatedAt
        };
    }

    public async Task<byte[]> DownloadTemplateAsync(Guid templateId)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Document template not found");
        }

        if (string.IsNullOrEmpty(template.FileUrl))
        {
            throw new FileNotFoundException("Template file not found");
        }

        // For now, return empty bytes as FileUrl is a path, not an ID
        // TODO: Implement proper file retrieval using file ID
        var fileBytes = new byte[0];
        return fileBytes;
    }

    public async Task<List<string>> GetPlaceholdersAsync(Guid templateId)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Document template not found");
        }

        if (string.IsNullOrEmpty(template.FileUrl))
        {
            return new List<string>();
        }

        try
        {
            // For now, return empty list as FileUrl is a path, not an ID
            // TODO: Implement proper file retrieval using file ID
            var fileBytes = new byte[0];
            var placeholders = ExtractPlaceholdersFromDocx(fileBytes);
            return placeholders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract placeholders from template {TemplateId}", templateId);
            return new List<string>();
        }
    }

    public async Task<TemplateValidationResult> ValidateTemplateAsync(Guid templateId, TemplateValidationRequest request)
    {
        try
        {
            var placeholders = await GetPlaceholdersAsync(templateId);
            var requiredFields = request.RequiredFields ?? new List<string>();

            var missingFields = requiredFields.Except(placeholders).ToList();
            var extraFields = placeholders.Except(requiredFields).ToList();

            return new TemplateValidationResult
            {
                Success = missingFields.Count == 0,
                Placeholders = placeholders,
                MissingFields = missingFields,
                ExtraFields = extraFields,
                Message = missingFields.Count == 0 ? "Template validation successful" : "Template missing required fields"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate template {TemplateId}", templateId);
            return new TemplateValidationResult
            {
                Success = false,
                Message = "Failed to validate template"
            };
        }
    }

    private List<string> ExtractPlaceholdersFromDocx(byte[] fileBytes)
    {
        var placeholders = new List<string>();

        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var document = WordprocessingDocument.Open(stream, false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null) return placeholders;

            // Extract placeholders from text content
            var textElements = body.Descendants<Text>();
            foreach (var text in textElements)
            {
                var textContent = text.Text;
                var matches = System.Text.RegularExpressions.Regex.Matches(textContent, @"\{\{([^}]+)\}\}");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var placeholder = match.Groups[1].Value.Trim();
                    if (!placeholders.Contains(placeholder))
                    {
                        placeholders.Add(placeholder);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract placeholders from DOCX file");
        }

        return placeholders;
    }
}
