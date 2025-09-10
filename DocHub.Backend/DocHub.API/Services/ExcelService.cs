using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace DocHub.API.Services;

public class ExcelService : IExcelService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<ExcelService> _logger;
    private readonly IFileStorageService _fileStorageService;

    public ExcelService(
        DocHubDbContext context,
        ILogger<ExcelService> logger,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    public async Task<ExcelUploadResult> UploadAsync(IFormFile file, Guid letterTypeDefinitionId, string? description = null)
    {
        try
        {
            // Validate letter type exists
            var letterType = await _context.LetterTypeDefinitions
                .FirstOrDefaultAsync(lt => lt.Id == letterTypeDefinitionId);

            if (letterType == null)
            {
                throw new ArgumentException("Letter type definition not found");
            }

            // Store file
            var filePath = await _fileStorageService.StoreFileAsync(file.OpenReadStream(), file.FileName, "excel-uploads");

            // Create upload record
            var upload = new ExcelUpload
            {
                Id = Guid.NewGuid(),
                LetterTypeDefinitionId = letterTypeDefinitionId,
                FileName = file.FileName,
                FilePath = filePath.FilePath,
                UploadedBy = "system", // TODO: Get from current user
                UploadedAt = DateTime.UtcNow,
                Metadata = new
                {
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    Description = description
                }.ToJson()
            };

            _context.ExcelUploads.Add(upload);
            await _context.SaveChangesAsync();

            // Parse Excel data
            var parseResult = await ParseExcelFileAsync(filePath.FilePath, letterTypeDefinitionId);

            return new ExcelUploadResult
            {
                Success = true,
                UploadId = upload.Id,
                FileName = upload.FileName,
                RowsProcessed = parseResult.RowsProcessed,
                Message = "Excel file uploaded and processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload Excel file");
            return new ExcelUploadResult
            {
                Success = false,
                Message = "Failed to upload Excel file"
            };
        }
    }

    public async Task<ExcelParseResult> ParseAsync(IFormFile file)
    {
        try
        {
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = await ParseExcelFileAsync(tempPath, null);

            // Clean up temp file
            File.Delete(tempPath);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Excel file");
            return new ExcelParseResult
            {
                Success = false,
                Message = "Failed to parse Excel file"
            };
        }
    }

    public async Task<List<ExcelUploadSummary>> GetUploadsAsync(Guid letterTypeDefinitionId)
    {
        var uploads = await _context.ExcelUploads
            .Where(u => u.LetterTypeDefinitionId == letterTypeDefinitionId)
            .OrderByDescending(u => u.UploadedAt)
            .Select(u => new ExcelUploadSummary
            {
                Id = u.Id,
                FileName = u.FileName,
                UploadedAt = u.UploadedAt,
                UploadedBy = u.UploadedBy
            })
            .ToListAsync();

        return uploads;
    }

    public async Task<ExcelDataResult> GetUploadDataAsync(Guid uploadId, int page = 1, int pageSize = 100)
    {
        var upload = await _context.ExcelUploads
            .FirstOrDefaultAsync(u => u.Id == uploadId);

        if (upload == null)
        {
            throw new ArgumentException("Excel upload not found");
        }

        // TODO: Implement actual data retrieval from stored Excel data
        // For now, return empty result
        return new ExcelDataResult
        {
            Success = true,
            Data = new List<Dictionary<string, object>>(),
            TotalRows = 0,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task DeleteUploadAsync(Guid uploadId)
    {
        var upload = await _context.ExcelUploads
            .FirstOrDefaultAsync(u => u.Id == uploadId);

        if (upload == null)
        {
            throw new ArgumentException("Excel upload not found");
        }

        // Delete file from storage
        // For now, skip file deletion as FilePath is a path, not an ID
        // TODO: Implement proper file deletion using file ID

        // Delete from database
        _context.ExcelUploads.Remove(upload);
        await _context.SaveChangesAsync();
    }

    public async Task<byte[]> DownloadTemplateAsync(Guid letterTypeDefinitionId)
    {
        var letterType = await _context.LetterTypeDefinitions
            .Include(lt => lt.DynamicFields)
            .FirstOrDefaultAsync(lt => lt.Id == letterTypeDefinitionId);

        if (letterType == null)
        {
            throw new ArgumentException("Letter type definition not found");
        }

        // Create Excel template with dynamic fields
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Data");

        // Add headers based on dynamic fields
        int col = 1;
        foreach (var field in letterType.DynamicFields.OrderBy(f => f.Order))
        {
            worksheet.Cells[1, col].Value = field.DisplayName;
            col++;
        }

        // Add sample data row
        int row = 2;
        col = 1;
        foreach (var field in letterType.DynamicFields.OrderBy(f => f.Order))
        {
            worksheet.Cells[row, col].Value = $"Sample {field.DisplayName}";
            col++;
        }

        return package.GetAsByteArray();
    }

    public Task<ExcelValidationResult> ValidateAsync(ExcelValidationRequest request)
    {
        try
        {
            // TODO: Implement actual validation logic
            return Task.FromResult(new ExcelValidationResult
            {
                Success = true,
                ValidRows = request.Data.Count,
                InvalidRows = 0,
                Errors = new List<string>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Excel data");
            return Task.FromResult(new ExcelValidationResult
            {
                Success = false,
                Message = "Failed to validate Excel data"
            });
        }
    }

    private async Task<ExcelParseResult> ParseExcelFileAsync(string filePath, Guid? letterTypeDefinitionId)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new ExcelParseResult
                {
                    Success = false,
                    Message = "No worksheets found in Excel file"
                };
            }

            var headers = new List<string>();
            var data = new List<Dictionary<string, object>>();

            // Get headers from first row
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString();
                if (!string.IsNullOrEmpty(header))
                {
                    headers.Add(header);
                }
            }

            // Get data from remaining rows
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= headers.Count; col++)
                {
                    var value = worksheet.Cells[row, col].Value;
                    rowData[headers[col - 1]] = value ?? string.Empty;
                }
                data.Add(rowData);
            }

            return new ExcelParseResult
            {
                Success = true,
                Headers = headers,
                Data = data,
                RowsProcessed = data.Count,
                Message = "Excel file parsed successfully"
            };
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Excel file at {FilePath}", filePath);
            return new ExcelParseResult
            {
                Success = false,
                Message = "Failed to parse Excel file"
            };
        }
    }
}
