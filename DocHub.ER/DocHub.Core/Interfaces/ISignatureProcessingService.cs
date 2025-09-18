using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Files;

namespace DocHub.Core.Interfaces;

public interface ISignatureProcessingService
{
    // Signature Processing
    Task<SignatureDto> ProcessSignatureAsync(ProcessSignatureRequest request, string userId);
    Task<byte[]> RemoveWatermarkAsync(byte[] signatureImage, WatermarkRemovalOptions options);
    Task<SignatureDto> InsertSignatureIntoDocumentAsync(InsertSignatureRequest request, string userId);
    Task<bool> ValidateSignatureQualityAsync(byte[] signatureImage);

    // Watermark Removal
    Task<byte[]> ProcessWatermarkRemovalAsync(byte[] imageData, WatermarkRemovalOptions options);
    Task<WatermarkRemovalOptions> DetectWatermarkAsync(byte[] imageData);
    Task<byte[]> OptimizeSignatureAsync(byte[] signatureData, string outputFormat = "png");

    // Signature Validation
    Task<bool> IsValidSignatureFormatAsync(byte[] imageData);
    Task<bool> IsSignatureQualityAcceptableAsync(byte[] imageData);
    Task<Dictionary<string, object>> AnalyzeSignatureAsync(byte[] imageData);
}
