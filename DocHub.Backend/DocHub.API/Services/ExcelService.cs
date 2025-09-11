using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

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
            .FirstOrDefaultAsync(lt => lt.Id == letterTypeDefinitionId);

        if (letterType == null)
        {
            throw new ArgumentException("Letter type definition not found");
        }

        using var memoryStream = new MemoryStream();
        using (var spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            // Add a WorkbookPart to the document
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            // Add a WorksheetPart to the WorkbookPart
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook
            var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());

            // Append a new worksheet and associate it with the workbook
            var sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Data"
            };
            sheets.Append(sheet);

            // Get the sheet data
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            // Add header row with standard fields
            var headerRow = new Row() { RowIndex = 1 };
            var standardFields = new[] { "EmployeeId", "EmployeeName", "Email", "Phone", "Department", "Position" };

            for (int i = 0; i < standardFields.Length; i++)
            {
                var cell = new Cell()
                {
                    CellReference = GetColumnName(i + 1) + "1",
                    DataType = CellValues.String,
                    CellValue = new CellValue(standardFields[i])
                };
                headerRow.AppendChild(cell);
            }
            sheetData.AppendChild(headerRow);

            // Add sample data row
            var dataRow = new Row() { RowIndex = 2 };
            for (int i = 0; i < standardFields.Length; i++)
            {
                var cell = new Cell()
                {
                    CellReference = GetColumnName(i + 1) + "2",
                    DataType = CellValues.String,
                    CellValue = new CellValue($"Sample {standardFields[i]}")
                };
                dataRow.AppendChild(cell);
            }
            sheetData.AppendChild(dataRow);

            // Add instructions row
            var instructionRow = new Row() { RowIndex = 3 };
            var instructionCell = new Cell()
            {
                CellReference = "A3",
                DataType = CellValues.String,
                CellValue = new CellValue("Instructions: Replace sample data with your actual data. Do not modify the header row.")
            };
            instructionRow.AppendChild(instructionCell);
            sheetData.AppendChild(instructionRow);
        }

        return memoryStream.ToArray();
    }

    private string GetColumnName(int columnNumber)
    {
        string columnName = "";
        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            columnNumber = (columnNumber - modulo) / 26;
        }
        return columnName;
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
                using var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false);
                var workbookPart = spreadsheetDocument.WorkbookPart;
                var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();

                if (worksheetPart == null)
                {
                    return new ExcelParseResult
                    {
                        Success = false,
                        Message = "No worksheets found in Excel file"
                    };
                }

                var worksheet = worksheetPart.Worksheet;
                var sheetData = worksheet.GetFirstChild<SheetData>();

                if (sheetData == null)
                {
                    return new ExcelParseResult
                    {
                        Success = false,
                        Message = "No data found in worksheet"
                    };
                }

                var headers = new List<string>();
                var data = new List<Dictionary<string, object>>();
                var rows = sheetData.Elements<Row>().ToList();

                if (rows.Count == 0)
                {
                    return new ExcelParseResult
                    {
                        Success = false,
                        Message = "Excel file is empty"
                    };
                }

                // Get headers from first row
                var headerRow = rows.FirstOrDefault();
                if (headerRow != null)
                {
                    var headerCells = headerRow.Elements<Cell>().ToList();
                    foreach (var cell in headerCells)
                    {
                        var cellValue = GetCellValue(cell, workbookPart);
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            headers.Add(cellValue);
                        }
                    }
                }

                // Get data from remaining rows
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowData = new Dictionary<string, object>();
                    var cells = row.Elements<Cell>().ToList();

                    for (int col = 0; col < headers.Count; col++)
                    {
                        var cell = cells.FirstOrDefault(c => GetColumnIndex(c.CellReference) == col);
                        var value = cell != null ? GetCellValue(cell, workbookPart) : string.Empty;
                        rowData[headers[col]] = value ?? string.Empty;
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

    private string GetCellValue(Cell cell, WorkbookPart workbookPart)
    {
        if (cell.CellValue == null)
            return string.Empty;

        string value = cell.CellValue.Text;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (sharedStringTable != null && int.TryParse(value, out int index))
            {
                var sharedStringItem = sharedStringTable.ElementAt(index);
                value = sharedStringItem?.InnerText ?? string.Empty;
            }
        }

        return value;
    }

    private int GetColumnIndex(string cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
            return 0;

        string columnPart = string.Empty;
        foreach (char c in cellReference)
        {
            if (char.IsLetter(c))
                columnPart += c;
            else
                break;
        }

        int columnIndex = 0;
        for (int i = 0; i < columnPart.Length; i++)
        {
            columnIndex = columnIndex * 26 + (columnPart[i] - 'A' + 1);
        }

        return columnIndex - 1; // Convert to 0-based index
    }
}
