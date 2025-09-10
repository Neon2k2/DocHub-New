using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface ISignatureService
{
    Task<List<SignatureSummary>> GetSignaturesAsync();
    Task<SignatureDetail> GetSignatureAsync(Guid signatureId);
    Task<SignatureSummary> CreateSignatureAsync(CreateSignatureRequest request);
    Task<SignatureSummary> UpdateSignatureAsync(Guid signatureId, UpdateSignatureRequest request);
    Task DeleteSignatureAsync(Guid signatureId);
    Task<SignatureSummary> UploadSignatureAsync(Guid signatureId, IFormFile file);
    Task<byte[]> DownloadSignatureAsync(Guid signatureId);
    Task<SignatureSummary> ProcessSignatureAsync(Guid signatureId);
    Task<string> GetPreviewUrlAsync(Guid signatureId);
}
