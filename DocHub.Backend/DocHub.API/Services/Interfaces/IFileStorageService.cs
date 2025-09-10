using DocHub.API.Models;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface IFileStorageService
{
    Task<FileStorage> StoreFileAsync(Stream fileStream, string fileName, string category, string? subCategory = null, bool isTemporary = false, DateTime? expiresAt = null);
    Task<byte[]> GetFileAsync(Guid fileId);
    Task<string> GetFileUrlAsync(Guid fileId);
    Task<bool> DeleteFileAsync(Guid fileId);
    Task<IEnumerable<FileStorage>> GetFilesByCategoryAsync(string category);
    Task CleanupExpiredFilesAsync();
    Task<FileStorageStatistics> GetStatisticsAsync();
}

public class FileStorageStatistics
{
    public int TotalFiles { get; set; }
    public long TotalStorageUsed { get; set; }
    public int ExpiredFiles { get; set; }
    public int TempFiles { get; set; }
    public Dictionary<string, long> StorageByCategory { get; set; } = new();
}
