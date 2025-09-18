using DocHub.Shared.DTOs.Excel;
using DocHub.Shared.DTOs.Files;
using Microsoft.AspNetCore.Http;

namespace DocHub.Core.Interfaces;

public interface IExcelProcessingService
{
    // Excel Processing
    Task<ExcelUploadDto> ProcessExcelFileAsync(ProcessExcelRequest request, string userId);
    Task<ExcelDataDto> ParseExcelDataAsync(Guid uploadId);
    Task<FieldMappingDto> SuggestFieldMappingAsync(FieldMappingRequest request);
    Task<ValidationResultDto> ValidateExcelDataAsync(ValidateExcelRequest request);
    Task<Stream> GenerateExcelTemplateAsync(GenerateTemplateRequest request);

    // Data Processing
    Task<IEnumerable<object>> ProcessExcelRowsAsync(Guid uploadId, string fieldMappings);
    Task<bool> ImportExcelDataAsync(Guid uploadId, Guid letterTypeId, string fieldMappings, string userId);
    Task<Dictionary<string, object>> AnalyzeExcelDataAsync(Guid uploadId);

    // Template Generation
    Task<Stream> GenerateExcelTemplateForLetterTypeAsync(Guid letterTypeId);
    Task<Stream> GenerateSampleExcelFileAsync(Guid letterTypeId, int sampleRows = 10);

    // Validation
    Task<bool> ValidateExcelFileAsync(IFormFile file);
    Task<ValidationResultDto> ValidateExcelStructureAsync(Guid uploadId, Guid letterTypeId);
    Task<Dictionary<string, object>> GetExcelMetadataAsync(Guid uploadId);

    // Additional methods used by controllers
    Task<ExcelUploadDto> UploadExcelAsync(UploadExcelRequest request, string userId);
    Task<ExcelProcessingResultDto> ProcessExcelAsync(Guid uploadId, ProcessExcelRequest request, string userId);
    Task<IEnumerable<ExcelUploadDto>> GetExcelUploadsAsync(Guid? letterTypeId = null);
    Task<ExcelUploadDto> GetExcelUploadAsync(Guid id);
    Task DeleteExcelUploadAsync(Guid id, string userId);
    Task<ExcelPreviewDto> PreviewExcelAsync(Guid id, int maxRows = 100);
    Task<IEnumerable<string>> GetExcelHeadersAsync(Guid id);
    Task<ExcelValidationResultDto> ValidateExcelAsync(Guid id, ValidateExcelRequest request);
    Task<FieldMappingResultDto> MapFieldsAsync(Guid id, MapFieldsRequest request);
    Task<ImportResultDto> ImportDataAsync(Guid id, ImportDataRequest request, string userId);
    Task<Stream> DownloadExcelAsync(Guid id, string format = "xlsx");
    Task<IEnumerable<ExcelTemplateDto>> GetExcelTemplatesAsync(Guid? letterTypeId = null);
    Task<ExcelTemplateDto> CreateExcelTemplateAsync(CreateExcelTemplateRequest request, string userId);
    Task<ExcelTemplateDto> UpdateExcelTemplateAsync(Guid id, UpdateExcelTemplateRequest request, string userId);
    Task DeleteExcelTemplateAsync(Guid id, string userId);
    Task<ExcelAnalyticsDto> GetExcelAnalyticsAsync(Guid? letterTypeId = null, DateTime? fromDate = null, DateTime? toDate = null);
    
    // Dynamic Table Methods
    Task<List<Dictionary<string, object>>> GetDataFromDynamicTableAsync(string tableName, int skip = 0, int take = 100);
}
