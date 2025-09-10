using DocHub.API.DTOs;

namespace DocHub.API.Services.Interfaces;

public interface IDocumentGenerationService
{
    Task<DocumentGenerationResult> GenerateBulkAsync(DocumentGenerationRequest request);
    Task<DocumentGenerationResult> GenerateSingleAsync(SingleDocumentGenerationRequest request);
    Task<DocumentPreviewResult> PreviewAsync(DocumentPreviewRequest request);
    Task<ValidationResult> ValidateAsync(DocumentValidationRequest request);
    Task<string> GetDownloadUrlAsync(Guid documentId, string format = "pdf");
    Task<byte[]> DownloadDocumentAsync(Guid documentId, string format = "pdf");
}