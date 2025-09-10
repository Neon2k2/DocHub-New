using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DocHub.API.Services;

public class ModuleService : IModuleService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<ModuleService> _logger;

    public ModuleService(DocHubDbContext context, ILogger<ModuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ModuleSummary>> GetModulesAsync()
    {
        var modules = await _context.Modules
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new ModuleSummary
            {
                Id = m.Id,
                Name = m.Name,
                DisplayName = m.DisplayName ?? string.Empty,
                Description = m.Description ?? string.Empty,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return modules;
    }

    public async Task<ModuleDetail> GetModuleAsync(Guid moduleId)
    {
        var module = await _context.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null)
        {
            throw new ArgumentException("Module not found");
        }

        return new ModuleDetail
        {
            Id = module.Id,
            Name = module.Name ?? string.Empty,
            DisplayName = module.DisplayName,
            Description = module.Description ?? string.Empty,
            IsActive = module.IsActive,
            CreatedAt = module.CreatedAt,
            UpdatedAt = module.UpdatedAt
        };
    }

    public async Task<ModuleSummary> CreateModuleAsync(CreateModuleRequest request)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        return new ModuleSummary
        {
            Id = module.Id,
            Name = module.Name ?? string.Empty,
            DisplayName = module.DisplayName,
            Description = module.Description ?? string.Empty,
            IsActive = module.IsActive,
            CreatedAt = module.CreatedAt
        };
    }

    public async Task<ModuleSummary> UpdateModuleAsync(Guid moduleId, UpdateModuleRequest request)
    {
        var module = await _context.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null)
        {
            throw new ArgumentException("Module not found");
        }

        module.Name = request.Name;
        module.DisplayName = request.DisplayName;
        module.Description = request.Description;
        module.IsActive = request.IsActive;
        module.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ModuleSummary
        {
            Id = module.Id,
            Name = module.Name ?? string.Empty,
            DisplayName = module.DisplayName,
            Description = module.Description ?? string.Empty,
            IsActive = module.IsActive,
            CreatedAt = module.CreatedAt
        };
    }

    public async Task DeleteModuleAsync(Guid moduleId)
    {
        var module = await _context.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null)
        {
            throw new ArgumentException("Module not found");
        }

        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();
    }

    public async Task<ModuleStatistics> GetModuleStatisticsAsync(Guid moduleId)
    {
        var module = await _context.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null)
        {
            throw new ArgumentException("Module not found");
        }

        var letterTypesCount = await _context.LetterTypeDefinitions
            .Where(lt => lt.ModuleId == moduleId)
            .CountAsync();

        var templatesCount = await _context.DocumentTemplates
            .Where(t => t.ModuleId == moduleId)
            .CountAsync();

        var signaturesCount = await _context.Signatures
            .Where(s => s.ModuleId == moduleId)
            .CountAsync();

        var generatedDocumentsCount = await _context.GeneratedDocuments
            .Where(gd => gd.ModuleId == moduleId)
            .CountAsync();

        var emailJobsCount = await _context.EmailJobs
            .Where(ej => ej.ModuleId == moduleId)
            .CountAsync();

        return new ModuleStatistics
        {
            ModuleId = moduleId,
            LetterTypesCount = letterTypesCount,
            TemplatesCount = templatesCount,
            SignaturesCount = signaturesCount,
            GeneratedDocumentsCount = generatedDocumentsCount,
            EmailJobsCount = emailJobsCount
        };
    }

    public async Task<List<LetterTypeSummary>> GetModuleLetterTypesAsync(Guid moduleId)
    {
        var letterTypes = await _context.LetterTypeDefinitions
            .Where(lt => lt.ModuleId == moduleId)
            .OrderBy(lt => lt.DisplayName)
            .Select(lt => new LetterTypeSummary
            {
                Id = lt.Id,
                TypeKey = lt.TypeKey,
                DisplayName = lt.DisplayName ?? string.Empty,
                Description = lt.Description ?? string.Empty,
                IsActive = lt.IsActive,
                CreatedAt = lt.CreatedAt
            })
            .ToListAsync();

        return letterTypes;
    }

    public async Task<List<ActivitySummary>> GetModuleActivitiesAsync(Guid moduleId, int limit = 10)
    {
        var activities = new List<ActivitySummary>();

        // Get recent letter type activities
        var letterTypeActivities = await _context.LetterTypeDefinitions
            .Where(lt => lt.ModuleId == moduleId)
            .OrderByDescending(lt => lt.UpdatedAt)
            .Take(limit / 2)
            .Select(lt => new ActivitySummary
            {
                Id = lt.Id,
                Type = "LetterType",
                Action = "Updated",
                Description = $"Letter type '{lt.DisplayName}' was updated",
                Timestamp = lt.UpdatedAt
            })
            .ToListAsync();

        activities.AddRange(letterTypeActivities);

        // Get recent document generation activities
        var documentActivities = await _context.GeneratedDocuments
            .Where(gd => gd.ModuleId == moduleId)
            .OrderByDescending(gd => gd.GeneratedAt)
            .Take(limit / 2)
            .Select(gd => new ActivitySummary
            {
                Id = gd.Id,
                Type = "Document",
                Action = "Generated",
                Description = $"Document '{gd.FileName}' was generated",
                Timestamp = gd.GeneratedAt
            })
            .ToListAsync();

        activities.AddRange(documentActivities);

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToList();
    }

    public async Task<ModuleDashboard> GetModuleDashboardAsync(Guid moduleId)
    {
        var module = await _context.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null)
        {
            throw new ArgumentException("Module not found");
        }

        var statistics = await GetModuleStatisticsAsync(moduleId);
        var activities = await GetModuleActivitiesAsync(moduleId, 5);
        var letterTypes = await GetModuleLetterTypesAsync(moduleId);

        return new ModuleDashboard
        {
            Module = new ModuleSummary
            {
                Id = module.Id,
                Name = module.Name ?? string.Empty,
                DisplayName = module.DisplayName,
                Description = module.Description ?? string.Empty,
                IsActive = module.IsActive,
                CreatedAt = module.CreatedAt
            },
            Statistics = statistics,
            RecentActivities = activities,
            LetterTypes = letterTypes
        };
    }
}
