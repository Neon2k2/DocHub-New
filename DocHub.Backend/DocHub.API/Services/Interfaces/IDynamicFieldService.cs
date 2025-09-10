using DocHub.API.Models;
using DocHub.API.DTOs;

namespace DocHub.API.Services.Interfaces;

public interface IDynamicFieldService
{
    Task<IEnumerable<DynamicField>> GetAllAsync();
    Task<DynamicField?> GetByIdAsync(Guid id);
    Task<IEnumerable<DynamicField>> GetByLetterTypeDefinitionIdAsync(Guid letterTypeDefinitionId);
    Task<DynamicField> CreateAsync(DynamicField field);
    Task<DynamicField> UpdateAsync(DynamicField field);
    Task DeleteAsync(Guid id);
    Task ReorderFieldsAsync(Guid letterTypeDefinitionId, List<FieldOrder> fieldOrders);
    Task<FieldValidationResult> ValidateFieldAsync(ValidateFieldRequest request);
    Task<bool> FieldKeyExistsAsync(Guid letterTypeDefinitionId, string fieldKey, Guid? excludeId = null);
    Task<IEnumerable<DynamicField>> GetRequiredFieldsAsync(Guid letterTypeDefinitionId);
    Task<Dictionary<string, object>> GetFieldDefaultsAsync(Guid letterTypeDefinitionId);
}
