using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Files;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace DocHub.Application.Services;

public class SignatureProcessingService : ISignatureProcessingService
{
    private readonly IRepository<Signature> _signatureRepository;
    private readonly IRepository<FileReference> _fileRepository;
    private readonly IFileManagementService _fileManagementService;
    private readonly ILogger<SignatureProcessingService> _logger;
    private readonly IDbContext _dbContext;

    public SignatureProcessingService(
        IRepository<Signature> signatureRepository,
        IRepository<FileReference> fileRepository,
        IFileManagementService fileManagementService,
        ILogger<SignatureProcessingService> logger,
        IDbContext dbContext)
    {
        _signatureRepository = signatureRepository;
        _fileRepository = fileRepository;
        _fileManagementService = fileManagementService;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<SignatureDto> ProcessSignatureAsync(ProcessSignatureRequest request, string userId)
    {
        try
        {
            // Upload the signature file first
            var uploadRequest = new UploadSignatureRequest
            {
                File = request.SignatureFile,
                Name = request.Name,
                Description = request.Description,
            };

            var signature = await _fileManagementService.UploadSignatureAsync(uploadRequest, userId);

            // Process watermark removal if needed
            if (request.WatermarkRemoval.Method != "none")
            {
                var fileInfo = await _fileManagementService.GetFileInfoAsync(signature.FileId);
                var fileStream = await _fileManagementService.DownloadFileAsync(signature.FileId);
                var imageBytes = await ReadStreamToBytesAsync(fileStream);
                
                var processedBytes = await ProcessWatermarkRemovalAsync(imageBytes, request.WatermarkRemoval);
                
                // Update the file with processed signature
                await UpdateSignatureFileAsync(signature.FileId, processedBytes);
            }

            _logger.LogInformation("Processed signature {Name} by user {UserId}", request.Name, userId);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature {Name}", request.Name);
            throw;
        }
    }

    public async Task<byte[]> RemoveWatermarkAsync(byte[] signatureImage, WatermarkRemovalOptions options)
    {
        try
        {
            return await ProcessWatermarkRemovalAsync(signatureImage, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing watermark from signature");
            throw;
        }
    }

    public async Task<SignatureDto> InsertSignatureIntoDocumentAsync(InsertSignatureRequest request, string userId)
    {
        try
        {
            // This would integrate with DocumentGenerationService
            // For now, return the signature info
        var signature = await _signatureRepository.GetFirstIncludingAsync(
            s => s.Id == request.SignatureId,
            s => s.File
        );

            if (signature == null)
            {
                throw new ArgumentException("Signature not found");
            }

            return new SignatureDto
            {
                Id = signature.Id,
                Name = signature.Name,
                Description = signature.Description,
                FileId = signature.FileId,
                FileName = signature.File?.FileName ?? string.Empty,
                IsActive = signature.IsActive,
                CreatedAt = signature.CreatedAt,
                UpdatedAt = signature.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting signature into document");
            throw;
        }
    }

    public async Task<bool> ValidateSignatureQualityAsync(byte[] signatureImage)
    {
        try
        {
            return await IsSignatureQualityAcceptableAsync(signatureImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature quality");
            return false;
        }
    }

    public async Task<byte[]> ProcessWatermarkRemovalAsync(byte[] imageData, WatermarkRemovalOptions options)
    {
        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);
            using var bitmap = new Bitmap(image);

            // Apply watermark removal based on method
            Bitmap processedBitmap = options.Method.ToLowerInvariant() switch
            {
                "automatic" => await RemoveWatermarkAutomaticAsync(bitmap, options),
                "manual" => await RemoveWatermarkManualAsync(bitmap, options),
                _ => bitmap
            };

            // Convert to desired format
            using var outputStream = new MemoryStream();
            var format = GetImageFormat(options.OutputFormat ?? "png");
            processedBitmap.Save(outputStream, format);
            
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing watermark removal");
            throw;
        }
    }

    public async Task<WatermarkRemovalOptions> DetectWatermarkAsync(byte[] imageData)
    {
        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);
            using var bitmap = new Bitmap(image);

            // Simple watermark detection logic
            var options = new WatermarkRemovalOptions
            {
                Method = "automatic",
                Threshold = 50,
                PreserveQuality = true,
                OutputFormat = "png"
            };

            // Analyze image for potential watermarks
            var hasWatermark = await DetectWatermarkInImageAsync(bitmap);
            if (hasWatermark)
            {
                options.Method = "automatic";
                options.Threshold = 60; // Higher threshold for detected watermarks
            }

            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting watermark");
            return new WatermarkRemovalOptions { Method = "automatic" };
        }
    }

    public async Task<byte[]> OptimizeSignatureAsync(byte[] signatureData, string outputFormat = "png")
    {
        try
        {
            _logger.LogDebug("🔧 [SIGNATURE_OPTIMIZE] Starting signature optimization");
            
            using var inputStream = new MemoryStream(signatureData);
            using var image = Image.FromStream(inputStream);
            
            // Resize if too large
            var maxSize = 800;
            if (image.Width > maxSize || image.Height > maxSize)
            {
                var ratio = Math.Min((double)maxSize / image.Width, (double)maxSize / image.Height);
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);
                
                using var resizedImage = new Bitmap(newWidth, newHeight);
                using var graphics = Graphics.FromImage(resizedImage);
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                
                using var outputStream = new MemoryStream();
                var format = GetImageFormat(outputFormat);
                resizedImage.Save(outputStream, format);
                
                _logger.LogDebug("✅ [SIGNATURE_OPTIMIZE] Signature resized and optimized");
                return await Task.FromResult(outputStream.ToArray());
            }

            _logger.LogDebug("✅ [SIGNATURE_OPTIMIZE] Signature already optimal size");
            return await Task.FromResult(signatureData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SIGNATURE_OPTIMIZE] Error optimizing signature");
            throw;
        }
    }

    public async Task<bool> IsValidSignatureFormatAsync(byte[] imageData)
    {
        try
        {
            _logger.LogDebug("🔍 [SIGNATURE_VALIDATE] Validating signature format");
            
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);
            
            // Check if it's a valid image format
            var validFormats = new[] { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Gif, ImageFormat.Bmp };
            var isValid = validFormats.Contains(image.RawFormat);
            
            _logger.LogDebug("✅ [SIGNATURE_VALIDATE] Format validation result: {IsValid}", isValid);
            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SIGNATURE_VALIDATE] Error validating signature format");
            return await Task.FromResult(false);
        }
    }

    public async Task<bool> IsSignatureQualityAcceptableAsync(byte[] imageData)
    {
        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);
            
            // Check minimum size requirements
            if (image.Width < 100 || image.Height < 50)
                return false;
            
            // Check if image has sufficient detail (simple check)
            using var bitmap = new Bitmap(image);
            var hasContent = await HasSufficientContentAsync(bitmap);
            
            return hasContent;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Dictionary<string, object>> AnalyzeSignatureAsync(byte[] imageData)
    {
        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);
            using var bitmap = new Bitmap(image);

            var analysis = new Dictionary<string, object>
            {
                ["width"] = image.Width,
                ["height"] = image.Height,
                ["format"] = image.RawFormat.ToString(),
                ["fileSize"] = imageData.Length,
                ["hasWatermark"] = await DetectWatermarkInImageAsync(bitmap),
                ["qualityScore"] = await CalculateQualityScoreAsync(bitmap),
                ["isValid"] = await IsValidSignatureFormatAsync(imageData),
                ["isQualityAcceptable"] = await IsSignatureQualityAcceptableAsync(imageData)
            };

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing signature");
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    private async Task<Bitmap> RemoveWatermarkAutomaticAsync(Bitmap bitmap, WatermarkRemovalOptions options)
    {
        _logger.LogDebug("🔧 [WATERMARK_REMOVE] Starting automatic watermark removal");
        
        // Simple automatic watermark removal using edge detection
        var processedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
        
        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                
                // Simple threshold-based removal
                if (pixel.R > options.Threshold && pixel.G > options.Threshold && pixel.B > options.Threshold)
                {
                    // Make pixel more transparent or white
                    processedBitmap.SetPixel(x, y, Color.White);
                }
                else
                {
                    processedBitmap.SetPixel(x, y, pixel);
                }
            }
        }
        
        _logger.LogDebug("✅ [WATERMARK_REMOVE] Automatic watermark removal completed");
        return await Task.FromResult(processedBitmap);
    }

    private async Task<Bitmap> RemoveWatermarkManualAsync(Bitmap bitmap, WatermarkRemovalOptions options)
    {
        _logger.LogDebug("🔧 [WATERMARK_REMOVE] Starting manual watermark removal");
        
        // Manual watermark removal based on coordinates
        if (string.IsNullOrEmpty(options.ManualCoordinates))
        {
            _logger.LogDebug("⚠️ [WATERMARK_REMOVE] No manual coordinates provided, returning original bitmap");
            return await Task.FromResult(bitmap);
        }

        try
        {
            var coordinates = JsonSerializer.Deserialize<Dictionary<string, int>>(options.ManualCoordinates);
            if (coordinates == null) return await Task.FromResult(bitmap);

            var x = coordinates.GetValueOrDefault("x", 0);
            var y = coordinates.GetValueOrDefault("y", 0);
            var width = coordinates.GetValueOrDefault("width", 0);
            var height = coordinates.GetValueOrDefault("height", 0);

            var processedBitmap = new Bitmap(bitmap);
            
            for (int i = x; i < Math.Min(x + width, bitmap.Width); i++)
            {
                for (int j = y; j < Math.Min(y + height, bitmap.Height); j++)
                {
                    processedBitmap.SetPixel(i, j, Color.White);
                }
            }
            
            _logger.LogDebug("✅ [WATERMARK_REMOVE] Manual watermark removal completed");
            return await Task.FromResult(processedBitmap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [WATERMARK_REMOVE] Error in manual watermark removal");
            return await Task.FromResult(bitmap);
        }
    }

    private async Task<bool> DetectWatermarkInImageAsync(Bitmap bitmap)
    {
        _logger.LogDebug("🔍 [WATERMARK_DETECT] Starting watermark detection");
        
        // Simple watermark detection based on color analysis
        var whitePixels = 0;
        var totalPixels = bitmap.Width * bitmap.Height;
        
        for (int x = 0; x < bitmap.Width; x += 10) // Sample every 10th pixel for performance
        {
            for (int y = 0; y < bitmap.Height; y += 10)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R > 200 && pixel.G > 200 && pixel.B > 200)
                {
                    whitePixels++;
                }
            }
        }
        
        var whitePixelRatio = (double)whitePixels / (totalPixels / 100); // Adjust for sampling
        var hasWatermark = whitePixelRatio > 0.3; // If more than 30% white pixels, likely has watermark
        
        _logger.LogDebug("✅ [WATERMARK_DETECT] Watermark detection completed: {HasWatermark}", hasWatermark);
        return await Task.FromResult(hasWatermark);
    }

    private async Task<bool> HasSufficientContentAsync(Bitmap bitmap)
    {
        _logger.LogDebug("🔍 [CONTENT_CHECK] Checking for sufficient content");
        
        // Check if image has enough non-white content
        var nonWhitePixels = 0;
        var totalPixels = bitmap.Width * bitmap.Height;
        
        for (int x = 0; x < bitmap.Width; x += 5)
        {
            for (int y = 0; y < bitmap.Height; y += 5)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R < 240 || pixel.G < 240 || pixel.B < 240)
                {
                    nonWhitePixels++;
                }
            }
        }
        
        var contentRatio = (double)nonWhitePixels / (totalPixels / 25); // Adjust for sampling
        var hasSufficientContent = contentRatio > 0.1; // At least 10% non-white content
        
        _logger.LogDebug("✅ [CONTENT_CHECK] Content check completed: {HasSufficientContent}", hasSufficientContent);
        return await Task.FromResult(hasSufficientContent);
    }

    private async Task<double> CalculateQualityScoreAsync(Bitmap bitmap)
    {
        _logger.LogDebug("📊 [QUALITY_SCORE] Calculating quality score");
        
        // Simple quality score based on image characteristics
        var score = 0.0;
        
        // Size score
        var sizeScore = Math.Min(1.0, (bitmap.Width * bitmap.Height) / (800.0 * 400.0));
        score += sizeScore * 0.3;
        
        // Content score
        var hasContent = await HasSufficientContentAsync(bitmap);
        score += hasContent ? 0.4 : 0.0;
        
        // Format score (PNG preferred)
        score += 0.3; // Assume good format
        
        var finalScore = Math.Min(1.0, score);
        _logger.LogDebug("✅ [QUALITY_SCORE] Quality score calculated: {Score}", finalScore);
        return await Task.FromResult(finalScore);
    }

    private ImageFormat GetImageFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "png" => ImageFormat.Png,
            "jpg" or "jpeg" => ImageFormat.Jpeg,
            "gif" => ImageFormat.Gif,
            "bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png
        };
    }

    private async Task<byte[]> ReadStreamToBytesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private async Task UpdateSignatureFileAsync(Guid fileId, byte[] processedBytes)
    {
        try
        {
            var fileReference = await _fileRepository.GetByIdAsync(fileId);
            if (fileReference == null) return;

            // Write processed bytes to file
            await File.WriteAllBytesAsync(fileReference.FilePath, processedBytes);
            
            // Update file size
            fileReference.FileSize = processedBytes.Length;
            await _fileRepository.UpdateAsync(fileReference);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating signature file {FileId}", fileId);
            throw;
        }
    }
}
