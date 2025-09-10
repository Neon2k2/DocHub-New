using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DocHub.API.Services;

public class FileStorageService : IFileStorageService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _baseStoragePath;

    public FileStorageService(
        DocHubDbContext context,
        ILogger<FileStorageService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _baseStoragePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
        
        // Ensure storage directory exists
        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<FileStorage> StoreFileAsync(Stream fileStream, string fileName, string category, string? subCategory = null, bool isTemporary = false, DateTime? expiresAt = null)
    {
        try
        {
            var fileId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{fileId}{fileExtension}";
            
            // Create category directory structure
            var categoryPath = Path.Combine(_baseStoragePath, category);
            if (!string.IsNullOrEmpty(subCategory))
            {
                categoryPath = Path.Combine(categoryPath, subCategory);
            }
            
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            var fullPath = Path.Combine(categoryPath, uniqueFileName);
            
            // Save file to disk
            using (var fileStreamWriter = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamWriter);
            }

            // Get file size
            var fileInfo = new FileInfo(fullPath);
            var fileSize = fileInfo.Length;

            // Create database record
            var fileStorage = new FileStorage
            {
                Id = fileId,
                FileName = fileName,
                FilePath = fullPath,
                ContentType = GetContentType(fileExtension),
                FileSize = fileSize,
                Category = category,
                SubCategory = subCategory,
                IsTemporary = isTemporary,
                ExpiresAt = expiresAt,
                UploadedAt = DateTime.UtcNow
            };

            _context.FileStorage.Add(fileStorage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File stored successfully: {FileName} in {Category}", fileName, category);
            return fileStorage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store file: {FileName}", fileName);
            throw new InvalidOperationException("Failed to store file", ex);
        }
    }

    public async Task<byte[]> GetFileAsync(Guid fileId)
    {
        try
        {
            var fileStorage = await _context.FileStorage
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileStorage == null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found");
            }

            if (!File.Exists(fileStorage.FilePath))
            {
                throw new FileNotFoundException($"Physical file not found: {fileStorage.FilePath}");
            }

            return await File.ReadAllBytesAsync(fileStorage.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file: {FileId}", fileId);
            throw;
        }
    }

    public async Task<string> GetFileUrlAsync(Guid fileId)
    {
        try
        {
            var fileStorage = await _context.FileStorage
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileStorage == null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found");
            }

            // In a real implementation, this would return a proper URL
            // For now, return a relative path that can be served by the API
            return $"/api/v1/files/download/{fileId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file URL: {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        try
        {
            var fileStorage = await _context.FileStorage
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileStorage == null)
            {
                return false;
            }

            // Delete physical file
            if (File.Exists(fileStorage.FilePath))
            {
                File.Delete(fileStorage.FilePath);
            }

            // Delete database record
            _context.FileStorage.Remove(fileStorage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File deleted successfully: {FileName}", fileStorage.FileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<IEnumerable<FileStorage>> GetFilesByCategoryAsync(string category)
    {
        try
        {
            return await _context.FileStorage
                .Where(f => f.Category == category)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files by category: {Category}", category);
            throw;
        }
    }

    public async Task CleanupExpiredFilesAsync()
    {
        try
        {
            var expiredFiles = await _context.FileStorage
                .Where(f => f.IsTemporary && f.ExpiresAt.HasValue && f.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            foreach (var file in expiredFiles)
            {
                // Delete physical file
                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }

                // Delete database record
                _context.FileStorage.Remove(file);
            }

            await _context.SaveChangesAsync();

            if (expiredFiles.Any())
            {
                _logger.LogInformation("Cleaned up {Count} expired files", expiredFiles.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired files");
            throw;
        }
    }

    public async Task<FileStorageStatistics> GetStatisticsAsync()
    {
        try
        {
            var files = await _context.FileStorage.ToListAsync();

            var statistics = new FileStorageStatistics
            {
                TotalFiles = files.Count,
                TotalStorageUsed = files.Sum(f => f.FileSize),
                ExpiredFiles = files.Count(f => f.IsTemporary && f.ExpiresAt.HasValue && f.ExpiresAt.Value < DateTime.UtcNow),
                TempFiles = files.Count(f => f.IsTemporary),
                StorageByCategory = files.GroupBy(f => f.Category)
                    .ToDictionary(g => g.Key, g => g.Sum(f => f.FileSize))
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file storage statistics");
            throw;
        }
    }

    // Overload for IFormFile
    public async Task<string> StoreFileAsync(IFormFile file, string category, string? subCategory = null, bool isTemporary = false, DateTime? expiresAt = null)
    {
        using var stream = file.OpenReadStream();
        var fileStorage = await StoreFileAsync(stream, file.FileName, category, subCategory, isTemporary, expiresAt);
        return fileStorage.FilePath;
    }

    // Overload for byte array
    public async Task<string> StoreFileAsync(byte[] fileBytes, string fileName, string category, string? subCategory = null, bool isTemporary = false, DateTime? expiresAt = null)
    {
        using var stream = new MemoryStream(fileBytes);
        var fileStorage = await StoreFileAsync(stream, fileName, category, subCategory, isTemporary, expiresAt);
        return fileStorage.FilePath;
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}
