using DocHub.Shared.DTOs.Tabs;
using DocHub.Shared.DTOs.Common;

namespace DocHub.Core.Interfaces;

public interface ITabManagementService
{
    // Letter Type Management
    Task<LetterTypeDefinitionDto> CreateLetterTypeAsync(CreateLetterTypeRequest request, string userId);
    Task<LetterTypeDefinitionDto> UpdateLetterTypeAsync(Guid id, UpdateLetterTypeRequest request, string userId);
    Task DeleteLetterTypeAsync(Guid id, string userId);
    Task<IEnumerable<LetterTypeDefinitionDto>> GetLetterTypesAsync();
    Task<LetterTypeDefinitionDto> GetLetterTypeAsync(Guid id);

    // Dynamic Field Management
    Task<DynamicFieldDto> CreateFieldAsync(CreateDynamicFieldRequest request, string userId);
    Task<DynamicFieldDto> UpdateFieldAsync(Guid id, UpdateDynamicFieldRequest request, string userId);
    Task DeleteFieldAsync(Guid id, string userId);
    Task<IEnumerable<DynamicFieldDto>> GetFieldsAsync(Guid letterTypeId);

    // Tab Data Management - Now handled by dynamic tables
    // Data is stored in dynamically created tables based on Excel uploads
}