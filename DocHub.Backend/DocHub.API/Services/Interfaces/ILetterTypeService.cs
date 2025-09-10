using DocHub.API.Models;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface ILetterTypeService
{
    Task<IEnumerable<LetterTypeDefinition>> GetAllAsync(string? module = null, bool? isActive = null);
    Task<LetterTypeDefinition?> GetByIdAsync(Guid id);
    Task<LetterTypeDefinition> CreateAsync(LetterTypeDefinition letterType);
    Task<LetterTypeDefinition?> UpdateAsync(Guid id, LetterTypeDefinition letterType);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string typeKey);
    Task<IEnumerable<LetterTypeDefinition>> GetByModuleAsync(string module);
}
