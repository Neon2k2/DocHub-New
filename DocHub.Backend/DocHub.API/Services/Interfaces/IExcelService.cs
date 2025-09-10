using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface IExcelService
{
    Task<ExcelUploadResult> UploadAsync(IFormFile file, Guid letterTypeDefinitionId, string? description = null);
    Task<ExcelParseResult> ParseAsync(IFormFile file);
    Task<List<ExcelUploadSummary>> GetUploadsAsync(Guid letterTypeDefinitionId);
    Task<ExcelDataResult> GetUploadDataAsync(Guid uploadId, int page = 1, int pageSize = 100);
    Task DeleteUploadAsync(Guid uploadId);
    Task<byte[]> DownloadTemplateAsync(Guid letterTypeDefinitionId);
    Task<ExcelValidationResult> ValidateAsync(ExcelValidationRequest request);
}
