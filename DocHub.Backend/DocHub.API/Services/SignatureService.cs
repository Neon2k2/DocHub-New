using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;

namespace DocHub.API.Services;

public class SignatureService : ISignatureService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<SignatureService> _logger;
    private readonly IFileStorageService _fileStorageService;

    public SignatureService(
        DocHubDbContext context,
        ILogger<SignatureService> logger,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    public async Task<List<SignatureSummary>> GetSignaturesAsync()
    {
        var signatures = await _context.Signatures
            .OrderBy(s => s.Name)
            .Select(s => new SignatureSummary
            {
                Id = s.Id,
                Name = s.Name,
                FileName = s.FileName,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return signatures;
    }

    public async Task<SignatureDetail> GetSignatureAsync(Guid signatureId)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        return new SignatureDetail
        {
            Id = signature.Id,
            Name = signature.Name,
            FileName = signature.FileName ?? string.Empty,
            FileUrl = signature.FileUrl ?? string.Empty,
            CreatedBy = signature.CreatedBy,
            CreatedAt = signature.CreatedAt,
            UpdatedAt = signature.UpdatedAt
        };
    }

    public async Task<SignatureSummary> CreateSignatureAsync(CreateSignatureRequest request)
    {
        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            Name = request.Name ?? string.Empty,
            FileName = request.FileName ?? string.Empty,
            FileUrl = request.FileUrl ?? string.Empty,
            CreatedBy = request.CreatedBy ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Signatures.Add(signature);
        await _context.SaveChangesAsync();

        return new SignatureSummary
        {
            Id = signature.Id,
            Name = signature.Name,
            FileName = signature.FileName ?? string.Empty,
            CreatedAt = signature.CreatedAt
        };
    }

    public async Task<SignatureSummary> UpdateSignatureAsync(Guid signatureId, UpdateSignatureRequest request)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        signature.Name = request.Name ?? string.Empty;
        signature.FileName = request.FileName ?? string.Empty;
        signature.FileUrl = request.FileUrl ?? string.Empty;
        signature.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new SignatureSummary
        {
            Id = signature.Id,
            Name = signature.Name,
            FileName = signature.FileName ?? string.Empty,
            CreatedAt = signature.CreatedAt
        };
    }

    public async Task DeleteSignatureAsync(Guid signatureId)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        // Delete file from storage if exists
        if (!string.IsNullOrEmpty(signature.FileUrl))
        {
            // For now, skip file deletion as FileUrl is a path, not an ID
            // TODO: Implement proper file deletion using file ID
        }

        _context.Signatures.Remove(signature);
        await _context.SaveChangesAsync();
    }

    public async Task<SignatureSummary> UploadSignatureAsync(Guid signatureId, IFormFile file)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        // Store file
        var fileStorage = await _fileStorageService.StoreFileAsync(file.OpenReadStream(), file.FileName, "signatures");

        // Update signature
        signature.FileName = file.FileName;
        signature.FileUrl = fileStorage.FilePath;
        signature.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new SignatureSummary
        {
            Id = signature.Id,
            Name = signature.Name,
            FileName = signature.FileName ?? string.Empty,
            CreatedAt = signature.CreatedAt
        };
    }

    public async Task<byte[]> DownloadSignatureAsync(Guid signatureId)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        if (string.IsNullOrEmpty(signature.FileUrl))
        {
            throw new FileNotFoundException("Signature file not found");
        }

        // For now, return empty bytes as FileUrl is a path, not an ID
        // TODO: Implement proper file retrieval using file ID
        var fileBytes = new byte[0];
        return fileBytes;
    }

    public async Task<SignatureSummary> ProcessSignatureAsync(Guid signatureId)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        if (string.IsNullOrEmpty(signature.FileUrl))
        {
            throw new InvalidOperationException("No signature file to process");
        }

        try
        {
            // Download original file
            // For now, use empty bytes as FileUrl is a path, not an ID
            // TODO: Implement proper file retrieval using file ID
            var originalBytes = new byte[0];
            
            // Process signature (remove watermark, optimize)
            var processedBytes = await ProcessSignatureImageAsync(originalBytes);
            
            // Store processed file
            var processedPath = await _fileStorageService.StoreFileAsync(
                new MemoryStream(processedBytes), 
                "signatures/processed", 
                $"{signatureId}_processed.png",
                "image/png");

            // Update signature with processed file
            signature.FileUrl = processedPath.FilePath;
            signature.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SignatureSummary
            {
                Id = signature.Id,
                Name = signature.Name,
                FileName = signature.FileName ?? string.Empty,
                CreatedAt = signature.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process signature {SignatureId}", signatureId);
            throw new InvalidOperationException("Failed to process signature");
        }
    }

    public async Task<string> GetPreviewUrlAsync(Guid signatureId)
    {
        var signature = await _context.Signatures
            .FirstOrDefaultAsync(s => s.Id == signatureId);

        if (signature == null)
        {
            throw new ArgumentException("Signature not found");
        }

        if (string.IsNullOrEmpty(signature.FileUrl))
        {
            throw new InvalidOperationException("No signature file available");
        }

        // Generate preview URL (in a real implementation, this would be a proper URL)
        return $"/api/v1/signatures/{signatureId}/preview";
    }

    private async Task<byte[]> ProcessSignatureImageAsync(byte[] originalBytes)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var originalStream = new MemoryStream(originalBytes);
                using var originalImage = Image.FromStream(originalStream);

            // Create a new image with transparent background
            using var processedImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(processedImage);

            // Set high quality rendering
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Draw original image
            graphics.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

            // TODO: Implement watermark removal logic
            // This would involve image processing to remove Adobe watermarks
            // and optimize the signature for document insertion

            // Convert to PNG bytes
            using var processedStream = new MemoryStream();
            processedImage.Save(processedStream, ImageFormat.Png);
            return processedStream.ToArray();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process signature image");
            throw new InvalidOperationException("Failed to process signature image");
        }
    }
}
