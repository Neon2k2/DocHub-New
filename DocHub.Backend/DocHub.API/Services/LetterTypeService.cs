using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DocHub.API.Services;

public class LetterTypeService : ILetterTypeService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<LetterTypeService> _logger;

    public LetterTypeService(DocHubDbContext context, ILogger<LetterTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<LetterTypeDefinition>> GetAllAsync(string? module = null, bool? isActive = null)
    {
        var query = _context.LetterTypeDefinitions
            .Include(lt => lt.Fields)
            .AsQueryable();

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(lt => lt.ModuleId.ToString() == module);
        }

        if (isActive.HasValue)
        {
            query = query.Where(lt => lt.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(lt => lt.DisplayName)
            .ToListAsync();
    }

    public async Task<LetterTypeDefinition?> GetByIdAsync(Guid id)
    {
        return await _context.LetterTypeDefinitions
            .Include(lt => lt.Fields)
            .FirstOrDefaultAsync(lt => lt.Id == id);
    }

    public async Task<LetterTypeDefinition> CreateAsync(LetterTypeDefinition letterType)
    {
        // Check if type key already exists
        if (await ExistsAsync(letterType.TypeKey))
        {
            throw new InvalidOperationException($"Letter type with key '{letterType.TypeKey}' already exists.");
        }

        letterType.CreatedAt = DateTime.UtcNow;
        letterType.UpdatedAt = DateTime.UtcNow;

        _context.LetterTypeDefinitions.Add(letterType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created letter type: {TypeKey} - {DisplayName}", 
            letterType.TypeKey, letterType.DisplayName);

        return letterType;
    }

    public async Task<LetterTypeDefinition?> UpdateAsync(Guid id, LetterTypeDefinition letterType)
    {
        var existingLetterType = await GetByIdAsync(id);
        if (existingLetterType == null)
        {
            return null;
        }

        // Check if type key is being changed and if it already exists
        if (existingLetterType.TypeKey != letterType.TypeKey && await ExistsAsync(letterType.TypeKey))
        {
            throw new InvalidOperationException($"Letter type with key '{letterType.TypeKey}' already exists.");
        }

        existingLetterType.TypeKey = letterType.TypeKey;
        existingLetterType.DisplayName = letterType.DisplayName;
        existingLetterType.Description = letterType.Description;
        existingLetterType.FieldConfiguration = letterType.FieldConfiguration;
        existingLetterType.IsActive = letterType.IsActive;
        existingLetterType.Module = letterType.Module;
        existingLetterType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated letter type: {TypeKey} - {DisplayName}", 
            letterType.TypeKey, letterType.DisplayName);

        return existingLetterType;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var letterType = await GetByIdAsync(id);
        if (letterType == null)
        {
            return false;
        }

        // Check if there are any generated documents or email jobs
        var hasGeneratedDocuments = await _context.GeneratedDocuments
            .AnyAsync(gd => gd.LetterTypeDefinitionId == id);

        var hasEmailJobs = await _context.EmailJobs
            .AnyAsync(ej => ej.LetterTypeDefinitionId == id);

        if (hasGeneratedDocuments || hasEmailJobs)
        {
            // Soft delete - just mark as inactive
            letterType.IsActive = false;
            letterType.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted letter type: {TypeKey} - {DisplayName}", 
                letterType.TypeKey, letterType.DisplayName);
        }
        else
        {
            // Hard delete
            _context.LetterTypeDefinitions.Remove(letterType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hard deleted letter type: {TypeKey} - {DisplayName}", 
                letterType.TypeKey, letterType.DisplayName);
        }

        return true;
    }

    public async Task<bool> ExistsAsync(string typeKey)
    {
        return await _context.LetterTypeDefinitions
            .AnyAsync(lt => lt.TypeKey == typeKey);
    }

    public async Task<IEnumerable<LetterTypeDefinition>> GetByModuleAsync(string module)
    {
        return await _context.LetterTypeDefinitions
            .Include(lt => lt.Fields)
            .Where(lt => lt.ModuleId.ToString() == module && lt.IsActive)
            .OrderBy(lt => lt.DisplayName)
            .ToListAsync();
    }
}
