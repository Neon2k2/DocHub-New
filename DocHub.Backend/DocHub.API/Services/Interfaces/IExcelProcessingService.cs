using DocHub.API.DTOs;
using DocHub.API.Models;

namespace DocHub.API.Services.Interfaces;

public interface IExcelProcessingService
{
    Task<ExcelProcessingResult> ProcessExcelFileAsync(IFormFile file, Guid letterTypeDefinitionId);
    Task<ExcelDataValidationResult> ValidateExcelDataAsync(IFormFile file, List<DynamicField> requiredFields);
    Task<List<Dictionary<string, object>>> ExtractDataFromExcelAsync(IFormFile file);
    Task<ExcelFieldMappingResult> MapExcelColumnsAsync(IFormFile file, List<DynamicField> targetFields);
}