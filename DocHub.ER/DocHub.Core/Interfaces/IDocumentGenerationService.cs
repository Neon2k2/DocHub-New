using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Files;

namespace DocHub.Core.Interfaces;

public interface IDocumentGenerationService
{
    // Document Generation
    Task<GeneratedDocumentDto> GenerateDocumentAsync(GenerateDocumentRequest request, string userId);
    Task<IEnumerable<GeneratedDocumentDto>> GenerateBulkDocumentsAsync(GenerateBulkDocumentsRequest request, string userId);
    Task<DocumentPreviewDto> PreviewDocumentAsync(PreviewDocumentRequest request, string userId);
    Task<Stream> DownloadDocumentAsync(Guid documentId, string format = "docx");

    // Template Processing
    Task<DocumentTemplateDto> ProcessTemplateAsync(ProcessTemplateRequest request, string userId);
    Task<IEnumerable<string>> ExtractPlaceholdersAsync(Guid templateId);
    Task<bool> ValidateTemplateAsync(Guid templateId);

    // Document Processing
    Task<GeneratedDocumentDto> ProcessDocumentAsync(ProcessDocumentRequest request, string userId);
    Task<byte[]> ConvertDocumentAsync(byte[] documentData, string fromFormat, string toFormat);
    Task<DocumentPreviewDto> GeneratePreviewAsync(byte[] documentData, string format);

    // Signature Processing
    Task<GeneratedDocumentDto> InsertSignatureIntoDocumentAsync(InsertSignatureRequest request, string userId);
    Task<byte[]> ProcessSignatureAsync(byte[] signatureData, WatermarkRemovalOptions options);
    Task<bool> ValidateSignatureQualityAsync(byte[] signatureData);
}
