using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Excel;
using DocHub.Shared.DTOs.Files;
using Microsoft.AspNetCore.Http;

namespace DocHub.Core.Interfaces;

public interface IFileManagementService
{
    // General File Operations
    Task<FileReferenceDto> UploadFileAsync(UploadFileRequest request, string userId);
    Task<Stream> DownloadFileAsync(Guid fileId);
    Task DeleteFileAsync(Guid fileId, string userId);
    Task<FileReferenceDto> GetFileInfoAsync(Guid fileId);
    Task<IEnumerable<FileReferenceDto>> GetFilesByCategoryAsync(string category);

    // Template Management
    Task<DocumentTemplateDto> UploadTemplateAsync(UploadTemplateRequest request, string userId);
    Task<IEnumerable<DocumentTemplateDto>> GetTemplatesAsync();
    Task<DocumentTemplateDto> GetTemplateAsync(Guid id);
    Task DeleteTemplateAsync(Guid id, string userId);
    Task<IEnumerable<string>> ExtractPlaceholdersAsync(Guid templateId);

    // Signature Management
    Task<SignatureDto> UploadSignatureAsync(UploadSignatureRequest request, string userId);
    Task<IEnumerable<SignatureDto>> GetSignaturesAsync();
    Task<SignatureDto> GetSignatureAsync(Guid id);
    Task DeleteSignatureAsync(Guid id, string userId);

    // Excel Management
    Task<ExcelUploadDto> UploadExcelAsync(UploadExcelRequest request, string userId);
    Task<IEnumerable<ExcelUploadDto>> GetExcelUploadsAsync(Guid? letterTypeId = null);
    Task<ExcelUploadDto> GetExcelUploadAsync(Guid id);

    // File Processing
    Task<byte[]> ProcessFileAsync(Guid fileId, string processingOptions);
    Task<bool> ValidateFileAsync(IFormFile file, string category);
    Task<string> GenerateFileHashAsync(Stream fileStream);
}
