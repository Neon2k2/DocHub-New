using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Files;
using DocHub.Shared.DTOs.Documents;
using DocHub.Shared.DTOs.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DocHub.Application.Services;

public class FileManagementService : IFileManagementService
{
    private readonly IRepository<FileReference> _fileRepository;
    private readonly IRepository<DocumentTemplate> _templateRepository;
    private readonly IRepository<Signature> _signatureRepository;
    private readonly IRepository<ExcelUpload> _excelRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<FileManagementService> _logger;
    private readonly IDbContext _dbContext;

    public FileManagementService(
        IRepository<FileReference> fileRepository,
        IRepository<DocumentTemplate> templateRepository,
        IRepository<Signature> signatureRepository,
        IRepository<ExcelUpload> excelRepository,
        IRepository<User> userRepository,
        ILogger<FileManagementService> logger,
        IDbContext dbContext)
    {
        _fileRepository = fileRepository;
        _templateRepository = templateRepository;
        _signatureRepository = signatureRepository;
        _excelRepository = excelRepository;
        _userRepository = userRepository;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<FileReferenceDto> UploadFileAsync(UploadFileRequest request, string userId)
    {
        try
        {
            // Validate file
            if (!await ValidateFileAsync(request.File, request.Category))
            {
                throw new ArgumentException("Invalid file format or size");
            }

            // Generate unique file path
            var fileExtension = Path.GetExtension(request.File.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine("uploads", request.Category, fileName);

            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // Validate user exists, fallback to admin user if not found
            var userGuid = Guid.Parse(userId);
            var userExists = await _userRepository.AnyAsync(u => u.Id == userGuid);
            if (!userExists)
            {
                _logger.LogWarning("User {UserId} not found, using admin user as fallback", userId);
                // Get admin user as fallback
                var adminUser = await _userRepository.FirstOrDefaultAsync(u => u.Email == "admin@collabera.com");
                if (adminUser != null)
                {
                    userGuid = adminUser.Id;
                    _logger.LogInformation("Using admin user {AdminUserId} as fallback", userGuid);
                }
                else
                {
                    _logger.LogError("Admin user not found, cannot proceed with file upload");
                    throw new InvalidOperationException("No valid user found for file upload");
                }
            }

            // Create file reference
            var fileReference = new FileReference
            {
                FileName = request.File.FileName,
                FilePath = filePath,
                FileSize = request.File.Length,
                MimeType = request.File.ContentType,
                Category = request.Category,
                SubCategory = request.SubCategory,
                UploadedBy = userGuid,
                ExpiresAt = request.ExpiresAt,
                IsTemporary = request.IsTemporary,
                Metadata = request.Metadata,
                ParentId = request.ParentId,
                ParentType = request.ParentType
            };

            await _fileRepository.AddAsync(fileReference);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Uploaded file {FileName} by user {UserId}", request.File.FileName, userId);

            return MapToFileReferenceDto(fileReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", request.File.FileName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(Guid fileId)
    {
        try
        {
            var fileReference = await _fileRepository.GetByIdAsync(fileId);
            if (fileReference == null || !fileReference.IsActive)
            {
                throw new ArgumentException("File not found");
            }

            if (!File.Exists(fileReference.FilePath))
            {
                throw new FileNotFoundException("File not found on disk");
            }

            return new FileStream(fileReference.FilePath, FileMode.Open, FileAccess.Read);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            throw;
        }
    }

    public async Task DeleteFileAsync(Guid fileId, string userId)
    {
        try
        {
            var fileReference = await _fileRepository.GetByIdAsync(fileId);
            if (fileReference == null)
            {
                throw new ArgumentException("File not found");
            }

            // Soft delete by setting IsActive to false
            fileReference.IsActive = false;

            // If it's a temporary file, also delete from disk
            if (fileReference.IsTemporary && File.Exists(fileReference.FilePath))
            {
                File.Delete(fileReference.FilePath);
            }

            await _fileRepository.UpdateAsync(fileReference);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted file {FileId} by user {UserId}", fileId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", fileId);
            throw;
        }
    }

    public async Task<FileReferenceDto> GetFileInfoAsync(Guid fileId)
    {
        try
        {
            var fileReference = await _fileRepository.GetFirstIncludingAsync(
                f => f.Id == fileId,
                f => f.UploadedByUser
            );

            if (fileReference == null)
            {
                throw new ArgumentException("File not found");
            }

            return MapToFileReferenceDto(fileReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info {FileId}", fileId);
            throw;
        }
    }

    public async Task<IEnumerable<FileReferenceDto>> GetFilesByCategoryAsync(string category)
    {
        try
        {
            var files = await _fileRepository.GetIncludingAsync(
                f => f.Category == category && f.IsActive,
                f => f.UploadedByUser
            );

            return files.Select(MapToFileReferenceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files by category {Category}", category);
            throw;
        }
    }

    public async Task<DocumentTemplateDto> UploadTemplateAsync(UploadTemplateRequest request, string userId)
    {
        try
        {
            // Upload the file first
            var fileRequest = new UploadFileRequest
            {
                File = request.File,
                Category = "template",
                SubCategory = request.Type,
                Metadata = request.Metadata
            };

            var fileReference = await UploadFileAsync(fileRequest, userId);

            // Create document template
            var template = new DocumentTemplate
            {
                Name = request.Name,
                Type = request.Type,
                FileId = fileReference.Id,
                Placeholders = request.Placeholders,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(template);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Uploaded template {Name} by user {UserId}", request.Name, userId);

            return await GetTemplateAsync(template.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template {Name}", request.Name);
            throw;
        }
    }

    public async Task<IEnumerable<DocumentTemplateDto>> GetTemplatesAsync()
    {
        try
        {
            var templates = await _templateRepository.GetIncludingAsync(
                t => t.IsActive,
                t => t.File
            );

            return templates.Select(MapToDocumentTemplateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            throw;
        }
    }

    public async Task<DocumentTemplateDto> GetTemplateAsync(Guid id)
    {
        try
        {
        var template = await _templateRepository.GetFirstIncludingAsync(
            t => t.Id == id,
            t => t.File
        );

            if (template == null)
            {
                throw new ArgumentException("Template not found");
            }

            return MapToDocumentTemplateDto(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {Id}", id);
            throw;
        }
    }

    public async Task DeleteTemplateAsync(Guid id, string userId)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template == null)
            {
                throw new ArgumentException("Template not found");
            }

            // Soft delete
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _templateRepository.UpdateAsync(template);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted template {Id} by user {UserId}", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ExtractPlaceholdersAsync(Guid templateId)
    {
        try
        {
            var template = await GetTemplateAsync(templateId);
            
            if (string.IsNullOrEmpty(template.Placeholders))
            {
                return new List<string>();
            }

            var placeholders = JsonSerializer.Deserialize<List<string>>(template.Placeholders);
            return placeholders ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting placeholders from template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<SignatureDto> UploadSignatureAsync(UploadSignatureRequest request, string userId)
    {
        try
        {
            // Upload the file first
            var fileRequest = new UploadFileRequest
            {
                File = request.File,
                Category = "signature",
                Metadata = JsonSerializer.Serialize(new { Description = request.Description })
            };

            var fileReference = await UploadFileAsync(fileRequest, userId);

            // Create signature
            var signature = new Signature
            {
                Name = request.Name,
                Description = request.Description,
                FileId = fileReference.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _signatureRepository.AddAsync(signature);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Uploaded signature {Name} by user {UserId}", request.Name, userId);

            return await GetSignatureAsync(signature.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading signature {Name}", request.Name);
            throw;
        }
    }

    public async Task<IEnumerable<SignatureDto>> GetSignaturesAsync()
    {
        try
        {
            var signatures = await _signatureRepository.GetIncludingAsync(
                s => s.IsActive,
                s => s.File
            );

            return signatures.Select(MapToSignatureDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signatures");
            throw;
        }
    }

    public async Task<SignatureDto> GetSignatureAsync(Guid id)
    {
        try
        {
        var signature = await _signatureRepository.GetFirstIncludingAsync(
            s => s.Id == id,
            s => s.File
        );

            if (signature == null)
            {
                throw new ArgumentException("Signature not found");
            }

            return MapToSignatureDto(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature {Id}", id);
            throw;
        }
    }

    public async Task DeleteSignatureAsync(Guid id, string userId)
    {
        try
        {
            var signature = await _signatureRepository.GetByIdAsync(id);
            if (signature == null)
            {
                throw new ArgumentException("Signature not found");
            }

            // Soft delete
            signature.IsActive = false;
            signature.UpdatedAt = DateTime.UtcNow;

            await _signatureRepository.UpdateAsync(signature);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted signature {Id} by user {UserId}", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting signature {Id}", id);
            throw;
        }
    }

    public async Task<ExcelUploadDto> UploadExcelAsync(UploadExcelRequest request, string userId)
    {
        try
        {
            // Upload the file first
            var fileRequest = new UploadFileRequest
            {
                File = request.File,
                Category = "excel",
                Metadata = request.ProcessingOptions
            };

            var fileReference = await UploadFileAsync(fileRequest, userId);

            // Create excel upload record
            var excelUpload = new ExcelUpload
            {
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                FileId = fileReference.Id,
                ProcessingOptions = request.ProcessingOptions,
                ProcessedBy = Guid.Parse(userId),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _excelRepository.AddAsync(excelUpload);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Uploaded Excel file for letter type {LetterTypeId} by user {UserId}", 
                request.LetterTypeDefinitionId, userId);

            return MapToExcelUploadDto(excelUpload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Excel file");
            throw;
        }
    }

    public async Task<IEnumerable<ExcelUploadDto>> GetExcelUploadsAsync(Guid? letterTypeId = null)
    {
        try
        {
            var uploads = await _excelRepository.GetIncludingAsync(
                e => letterTypeId == null || e.LetterTypeDefinitionId == letterTypeId,
                e => e.LetterTypeDefinition,
                e => e.File,
                e => e.ProcessedByUser
            );

            return uploads.Select(MapToExcelUploadDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel uploads");
            throw;
        }
    }

    public async Task<ExcelUploadDto> GetExcelUploadAsync(Guid id)
    {
        try
        {
        var upload = await _excelRepository.GetFirstIncludingAsync(
            e => e.Id == id,
            e => e.LetterTypeDefinition,
            e => e.File,
            e => e.ProcessedByUser
        );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            return MapToExcelUploadDto(upload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel upload {Id}", id);
            throw;
        }
    }

    public async Task<byte[]> ProcessFileAsync(Guid fileId, string processingOptions)
    {
        try
        {
            var fileReference = await _fileRepository.GetByIdAsync(fileId);
            if (fileReference == null || !fileReference.IsActive)
            {
                throw new ArgumentException("File not found");
            }

            if (!File.Exists(fileReference.FilePath))
            {
                throw new FileNotFoundException("File not found on disk");
            }

            // Read file content
            var fileBytes = await File.ReadAllBytesAsync(fileReference.FilePath);

            // Apply processing based on options
            var options = JsonSerializer.Deserialize<Dictionary<string, object>>(processingOptions ?? "{}");
            
            // Add your file processing logic here based on the options
            // This could include image resizing, format conversion, etc.

            return fileBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FileId}", fileId);
            throw;
        }
    }

    public Task<bool> ValidateFileAsync(IFormFile file, string category)
    {
        try
        {
            // Check file size (max 50MB)
            if (file.Length > 50 * 1024 * 1024)
            {
                return Task.FromResult(false);
            }

            // Check file extension based on category
            var allowedExtensions = category switch
            {
                "template" => new[] { ".docx", ".doc", ".pdf" },
                "signature" => new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" },
                "excel" => new[] { ".xlsx", ".xls", ".csv" },
                "document" => new[] { ".pdf", ".docx", ".doc" },
                _ => new[] { ".txt" }
            };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return Task.FromResult(allowedExtensions.Contains(fileExtension));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file {FileName}", file.FileName);
            return Task.FromResult(false);
        }
    }

    public async Task<string> GenerateFileHashAsync(Stream fileStream)
    {
        try
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating file hash");
            throw;
        }
    }

    private FileReferenceDto MapToFileReferenceDto(FileReference fileReference)
    {
        return new FileReferenceDto
        {
            Id = fileReference.Id,
            FileName = fileReference.FileName,
            FilePath = fileReference.FilePath,
            FileSize = fileReference.FileSize,
            MimeType = fileReference.MimeType,
            Category = fileReference.Category,
            SubCategory = fileReference.SubCategory,
            UploadedBy = fileReference.UploadedBy,
            UploadedByName = fileReference.UploadedByUser?.Username ?? string.Empty,
            UploadedAt = fileReference.UploadedAt,
            ExpiresAt = fileReference.ExpiresAt,
            IsTemporary = fileReference.IsTemporary,
            IsActive = fileReference.IsActive,
            Version = fileReference.Version,
            Metadata = fileReference.Metadata,
            Placeholders = fileReference.Placeholders,
            ParentId = fileReference.ParentId,
            ParentType = fileReference.ParentType
        };
    }

    private DocumentTemplateDto MapToDocumentTemplateDto(DocumentTemplate template)
    {
        return new DocumentTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Type = template.Type,
            FileId = template.FileId,
            FileName = template.File?.FileName ?? string.Empty,
            Placeholders = template.Placeholders,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private SignatureDto MapToSignatureDto(Signature signature)
    {
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

    private ExcelUploadDto MapToExcelUploadDto(ExcelUpload upload)
    {
        return new ExcelUploadDto
        {
            Id = upload.Id,
            LetterTypeDefinitionId = upload.LetterTypeDefinitionId,
            LetterTypeName = upload.LetterTypeDefinition?.DisplayName ?? string.Empty,
            FileId = upload.FileId,
            FileName = upload.File?.FileName ?? string.Empty,
            Metadata = upload.Metadata,
            ParsedData = upload.ParsedData,
            FieldMappings = upload.FieldMappings,
            ProcessingOptions = upload.ProcessingOptions,
            Results = upload.Results,
            IsProcessed = upload.IsProcessed,
            ProcessedRows = upload.ProcessedRows,
            ProcessedBy = upload.ProcessedBy,
            ProcessedByName = upload.ProcessedByUser?.Username ?? string.Empty,
            CreatedAt = upload.CreatedAt,
            UpdatedAt = upload.UpdatedAt
        };
    }
}