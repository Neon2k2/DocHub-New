using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Excel;
using DocHub.Shared.DTOs.Files;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Syncfusion.XlsIO;

namespace DocHub.Application.Services;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly IRepository<ExcelUpload> _excelRepository;
    private readonly IRepository<LetterTypeDefinition> _letterTypeRepository;
    private readonly IFileManagementService _fileManagementService;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<ExcelProcessingService> _logger;
    private readonly IDbContext _dbContext;

    public ExcelProcessingService(
        IRepository<ExcelUpload> excelRepository,
        IRepository<LetterTypeDefinition> letterTypeRepository,
        IFileManagementService fileManagementService,
        IDynamicTableService dynamicTableService,
        ILogger<ExcelProcessingService> logger,
        IDbContext dbContext)
    {
        _excelRepository = excelRepository;
        _letterTypeRepository = letterTypeRepository;
        _fileManagementService = fileManagementService;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<ExcelUploadDto> ProcessExcelFileAsync(ProcessExcelRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("🚀 [EXCEL-PROCESS] Starting Excel file processing for upload {UploadId}", request.UploadId);

            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == request.UploadId,
                e => e.File,
                e => e.LetterTypeDefinition
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            // Parse Excel data
            var excelData = await ParseExcelDataAsync(request.UploadId);
            _logger.LogInformation("📊 [EXCEL-PROCESS] Parsed {RowCount} rows from Excel", excelData.Rows.Count);

            // Detect column types from the data
            var columnDefinitions = await _dynamicTableService.DetectColumnTypesAsync(excelData.Rows);
            _logger.LogInformation("🔍 [EXCEL-PROCESS] Detected {ColumnCount} columns", columnDefinitions.Count);

            // Create dynamic table
            var tableName = await _dynamicTableService.CreateDynamicTableAsync(
                upload.LetterTypeDefinitionId, 
                request.UploadId, 
                columnDefinitions, 
                excelData.Rows
            );

            _logger.LogInformation("✅ [EXCEL-PROCESS] Created dynamic table: {TableName}", tableName);

            // Update upload with table metadata
            upload.ParsedData = JsonSerializer.Serialize(new { 
                TableName = tableName, 
                TotalRows = excelData.Rows.Count,
                Columns = columnDefinitions.Select(c => new { c.ColumnName, c.DataType }).ToList()
            });
            upload.IsProcessed = true;
            upload.ProcessedRows = excelData.Rows.Count;
            upload.ProcessedBy = await ValidateUserIdAsync(userId);
            upload.UpdatedAt = DateTime.UtcNow;

            await _excelRepository.UpdateAsync(upload);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("🎉 [EXCEL-PROCESS] Successfully processed Excel file {UploadId} into dynamic table {TableName}", request.UploadId, tableName);

            return MapToExcelUploadDto(upload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-PROCESS] Error processing Excel file {UploadId}", request.UploadId);
            throw;
        }
    }

    public async Task<ExcelDataDto> ParseExcelDataAsync(Guid uploadId)
    {
        try
        {
            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == uploadId,
                e => e.File
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            // Read Excel file
            var fileStream = await _fileManagementService.DownloadFileAsync(upload.FileId);
            var excelData = await ParseExcelStreamAsync(fileStream);

            return excelData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Excel data for upload {UploadId}", uploadId);
            throw;
        }
    }

    public async Task<FieldMappingDto> SuggestFieldMappingAsync(FieldMappingRequest request)
    {
        try
        {
            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == request.UploadId,
                e => e.LetterTypeDefinition
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            var letterType = upload.LetterTypeDefinition;
            var dynamicFields = letterType.DynamicFields?.Where(f => f.IsActive).ToList() ?? new List<DynamicField>();

            // Parse Excel headers
            var excelData = await ParseExcelDataAsync(request.UploadId);
            var headers = excelData.Headers;

            // Suggest mappings based on field names and headers
            var mappings = new List<FieldMappingDto>();

            foreach (var field in dynamicFields)
            {
                var bestMatch = FindBestHeaderMatch(field, headers);
                if (bestMatch != null)
                {
                    mappings.Add(new FieldMappingDto
                    {
                        ExcelColumn = bestMatch,
                        DynamicField = field.FieldKey,
                        FieldType = field.FieldType,
                        IsRequired = field.IsRequired,
                        ValidationRules = field.ValidationRules,
                        Confidence = CalculateMatchConfidence(field, bestMatch)
                    });
                }
            }

            return new FieldMappingDto
            {
                ExcelColumn = "suggested_mappings",
                DynamicField = "mappings",
                FieldType = "object",
                IsRequired = false,
                Confidence = 1.0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting field mappings for upload {UploadId}", request.UploadId);
            throw;
        }
    }

    public async Task<ValidationResultDto> ValidateExcelDataAsync(ValidateExcelRequest request)
    {
        try
        {
            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == request.UploadId,
                e => e.LetterTypeDefinition
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            var excelData = await ParseExcelDataAsync(request.UploadId);
            var letterType = upload.LetterTypeDefinition;
            var dynamicFields = letterType.DynamicFields?.Where(f => f.IsActive).ToList() ?? new List<DynamicField>();

            var validationResult = new ValidationResultDto
            {
                TotalRows = excelData.Rows.Count,
                ValidRows = 0,
                InvalidRows = 0,
                Errors = new List<ValidationErrorDto>(),
                Warnings = new List<ValidationWarningDto>()
            };

            // Validate each row
            for (int i = 0; i < excelData.Rows.Count; i++)
            {
                var row = excelData.Rows[i];
                var rowNumber = i + 1;
                var isValid = true;

                foreach (var field in dynamicFields)
                {
                    if (field.IsRequired && !row.ContainsKey(field.FieldKey))
                    {
                        validationResult.Errors.Add(new ValidationErrorDto
                        {
                            RowNumber = rowNumber,
                            Field = field.FieldKey,
                            Message = $"Required field '{field.DisplayName}' is missing",
                            ErrorType = "required"
                        });
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    validationResult.ValidRows++;
                }
                else
                {
                    validationResult.InvalidRows++;
                }
            }

            validationResult.IsValid = validationResult.InvalidRows == 0;

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel data for upload {UploadId}", request.UploadId);
            throw;
        }
    }

    public async Task<Stream> GenerateExcelTemplateAsync(GenerateTemplateRequest request)
    {
        try
        {
        var letterType = await _letterTypeRepository.GetFirstIncludingAsync(
            lt => lt.Id == request.LetterTypeDefinitionId,
            lt => lt.DynamicFields
        );

            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            var dynamicFields = letterType.DynamicFields?.Where(f => f.IsActive).OrderBy(f => f.OrderIndex).ToList() ?? new List<DynamicField>();

            // Generate Excel template with headers
            var headers = dynamicFields.Select(f => f.DisplayName).ToList();
            var sampleData = GenerateSampleData(dynamicFields, 5); // 5 sample rows

            // Create Excel file (simplified - in real implementation, use EPPlus or similar)
            var templateData = CreateExcelTemplate(headers, sampleData);
            var stream = new MemoryStream(templateData);

            _logger.LogInformation("Generated Excel template for letter type {LetterTypeId}", request.LetterTypeDefinitionId);

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel template for letter type {LetterTypeId}", request.LetterTypeDefinitionId);
            throw;
        }
    }

    public async Task<IEnumerable<object>> ProcessExcelRowsAsync(Guid uploadId, string fieldMappings)
    {
        try
        {
        var upload = await _excelRepository.GetFirstIncludingAsync(
            e => e.Id == uploadId,
            e => e.LetterTypeDefinition
        );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            var excelData = await ParseExcelDataAsync(uploadId);
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldMappings) ?? new Dictionary<string, string>();

            var processedRows = new List<object>();

            foreach (var row in excelData.Rows)
            {
                var processedRow = new Dictionary<string, object>();
                
                // If no field mappings provided, use the original Excel data as-is
                if (mappings.Count == 0)
                {
                    foreach (var kvp in row)
                    {
                        processedRow[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Apply field mappings
                    foreach (var mapping in mappings)
                    {
                        if (row.ContainsKey(mapping.Key))
                        {
                            processedRow[mapping.Value] = row[mapping.Key];
                        }
                    }
                }

                processedRows.Add(processedRow);
            }

            return processedRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel rows for upload {UploadId}", uploadId);
            throw;
        }
    }

    public async Task<bool> ImportExcelDataAsync(Guid uploadId, Guid letterTypeId, string fieldMappings, string userId)
    {
        try
        {
            _logger.LogInformation("🚀 [EXCEL-IMPORT] Starting dynamic table import for upload {UploadId}", uploadId);

            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == uploadId,
                e => e.LetterTypeDefinition
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            var letterType = await _letterTypeRepository.GetByIdAsync(letterTypeId);
            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            // Get the parsed Excel data
            var excelData = await ParseExcelDataAsync(uploadId);
            _logger.LogInformation("📊 [EXCEL-IMPORT] Parsed {RowCount} rows from Excel", excelData.Rows.Count);

            // Find existing dynamic table for this letter type
            var existingTable = await _dynamicTableService.GetTableNameForLetterTypeAsync(letterTypeId);
            if (string.IsNullOrEmpty(existingTable))
            {
                throw new InvalidOperationException($"No dynamic table found for letter type {letterTypeId}. Please create the tab with columns first.");
            }

            _logger.LogInformation("🔍 [EXCEL-IMPORT] Found existing table: {TableName}", existingTable);

            // Insert data into existing table
            var success = await _dynamicTableService.InsertDataIntoDynamicTableAsync(existingTable, excelData.Rows);
            if (!success)
            {
                throw new InvalidOperationException("Failed to insert data into existing table");
            }

            _logger.LogInformation("✅ [EXCEL-IMPORT] Inserted data into existing table: {TableName}", existingTable);

            // Update ExcelUpload to mark as processed
            upload.IsProcessed = true;
            upload.ProcessedBy = await ValidateUserIdAsync(userId);
            upload.UpdatedAt = DateTime.UtcNow;
            upload.ParsedData = JsonSerializer.Serialize(new { 
                TableName = existingTable, 
                TotalRows = excelData.Rows.Count,
                Message = "Data inserted into existing table"
            });

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("🎉 [EXCEL-IMPORT] Successfully imported {RowCount} rows into dynamic table {TableName}", 
                excelData.Rows.Count, existingTable);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-IMPORT] Error importing Excel data for upload {UploadId}", uploadId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> AnalyzeExcelDataAsync(Guid uploadId)
    {
        try
        {
            var excelData = await ParseExcelDataAsync(uploadId);

            var analysis = new Dictionary<string, object>
            {
                ["total_rows"] = excelData.Rows.Count,
                ["total_columns"] = excelData.Headers.Count,
                ["headers"] = excelData.Headers,
                ["sample_data"] = excelData.Rows.Take(5).ToList(),
                ["column_types"] = AnalyzeColumnTypes(excelData.Rows),
                ["empty_cells"] = CountEmptyCells(excelData.Rows),
                ["duplicate_rows"] = CountDuplicateRows(excelData.Rows)
            };

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing Excel data for upload {UploadId}", uploadId);
            throw;
        }
    }

    public async Task<Stream> GenerateExcelTemplateForLetterTypeAsync(Guid letterTypeId)
    {
        try
        {
            var request = new GenerateTemplateRequest
            {
                LetterTypeDefinitionId = letterTypeId
            };

            return await GenerateExcelTemplateAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel template for letter type {LetterTypeId}", letterTypeId);
            throw;
        }
    }

    public async Task<Stream> GenerateSampleExcelFileAsync(Guid letterTypeId, int sampleRows = 10)
    {
        try
        {
        var letterType = await _letterTypeRepository.GetFirstIncludingAsync(
            lt => lt.Id == letterTypeId,
            lt => lt.DynamicFields
        );

            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            var dynamicFields = letterType.DynamicFields?.Where(f => f.IsActive).OrderBy(f => f.OrderIndex).ToList() ?? new List<DynamicField>();
            var headers = dynamicFields.Select(f => f.DisplayName).ToList();
            var sampleData = GenerateSampleData(dynamicFields, sampleRows);

            var templateData = CreateExcelTemplate(headers, sampleData);
            return new MemoryStream(templateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sample Excel file for letter type {LetterTypeId}", letterTypeId);
            throw;
        }
    }

    public Task<bool> ValidateExcelFileAsync(IFormFile file)
    {
        try
        {
            // Check file extension
            var allowedExtensions = new[] { ".xlsx", ".xls", ".csv" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Task.FromResult(false);
            }

            // Check file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel file {FileName}", file.FileName);
            return Task.FromResult(false);
        }
    }

    public async Task<ValidationResultDto> ValidateExcelStructureAsync(Guid uploadId, Guid letterTypeId)
    {
        try
        {
        var upload = await _excelRepository.GetFirstIncludingAsync(
            e => e.Id == uploadId,
            e => e.LetterTypeDefinition
        );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

        var letterType = await _letterTypeRepository.GetFirstIncludingAsync(
            lt => lt.Id == letterTypeId,
            lt => lt.DynamicFields
        );

            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            var excelData = await ParseExcelDataAsync(uploadId);
            var requiredFields = letterType.DynamicFields?.Where(f => f.IsActive).Select(f => f.FieldKey).ToList() ?? new List<string>();

            var validationResult = new ValidationResultDto
            {
                TotalRows = excelData.Rows.Count,
                ValidRows = 0,
                InvalidRows = 0,
                Errors = new List<ValidationErrorDto>(),
                Warnings = new List<ValidationWarningDto>()
            };

            // Check if all required fields are present in headers
            var missingFields = requiredFields.Where(f => !excelData.Headers.Contains(f)).ToList();
            foreach (var field in missingFields)
            {
                validationResult.Errors.Add(new ValidationErrorDto
                {
                    RowNumber = 0,
                    Field = field,
                    Message = $"Required field '{field}' is missing from Excel headers",
                    ErrorType = "missing_header"
                });
            }

            validationResult.IsValid = validationResult.Errors.Count == 0;
            validationResult.ValidRows = validationResult.IsValid ? excelData.Rows.Count : 0;
            validationResult.InvalidRows = excelData.Rows.Count - validationResult.ValidRows;

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel structure for upload {UploadId}", uploadId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetExcelMetadataAsync(Guid uploadId)
    {
        try
        {
            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == uploadId,
                e => e.File,
                e => e.LetterTypeDefinition
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            var metadata = new Dictionary<string, object>
            {
                ["upload_id"] = upload.Id,
                ["file_name"] = upload.File?.FileName ?? string.Empty,
                ["file_size"] = upload.File?.FileSize ?? 0,
                ["letter_type"] = upload.LetterTypeDefinition?.DisplayName ?? string.Empty,
                ["is_processed"] = upload.IsProcessed,
                ["processed_rows"] = upload.ProcessedRows,
                ["created_at"] = upload.CreatedAt,
                ["updated_at"] = upload.UpdatedAt
            };

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel metadata for upload {UploadId}", uploadId);
            throw;
        }
    }

    private async Task<ExcelDataDto> ParseExcelStreamAsync(Stream stream)
    {
        try
        {
            var rows = new List<Dictionary<string, object>>();
            var headers = new List<string>();

            // Use Syncfusion to read Excel files
            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
            
            var workbook = application.Workbooks.Open(stream);
            var worksheet = workbook.Worksheets[0];
            
            // Get the used range
            var usedRange = worksheet.UsedRange;
            if (usedRange == null || usedRange.Rows.Count() == 0)
            {
                return await Task.FromResult(new ExcelDataDto
                {
                    UploadId = Guid.Empty,
                    Rows = rows,
                    Headers = headers,
                    TotalRows = 0,
                    IsValid = true
                });
            }

            // Read headers from first row
            for (int col = 1; col <= usedRange.Columns.Count(); col++)
            {
                var cell = worksheet.Range[1, col];
                string headerValue;
                
                // Try to get the actual value first, then fall back to text
                if (cell.Value2 != null)
                {
                    headerValue = cell.Value2.ToString()?.Trim() ?? string.Empty;
                }
                else if (!string.IsNullOrEmpty(cell.Text))
                {
                    headerValue = cell.Text.Trim();
                }
                else
                {
                    headerValue = string.Empty;
                }
                
                if (!string.IsNullOrEmpty(headerValue))
                {
                    headers.Add(headerValue);
                }
            }

            // Read data rows
            for (int row = 2; row <= usedRange.Rows.Count(); row++)
            {
                var dataRow = new Dictionary<string, object>();
                
                for (int col = 1; col <= headers.Count; col++)
                {
                    var cell = worksheet.Range[row, col];
                    object cellValue;
                    
                    // Try to get the actual value first, then fall back to text
                    if (cell.Value2 != null)
                    {
                        cellValue = cell.Value2;
                    }
                    else if (!string.IsNullOrEmpty(cell.Text))
                    {
                        cellValue = cell.Text.Trim();
                    }
                    else
                    {
                        cellValue = string.Empty;
                    }
                    
                    dataRow[headers[col - 1]] = cellValue;
                }
                
                rows.Add(dataRow);
            }

            workbook.Close();
            excelEngine.Dispose();

            return await Task.FromResult(new ExcelDataDto
            {
                UploadId = Guid.Empty, // Will be set by caller
                Rows = rows,
                Headers = headers,
                TotalRows = rows.Count,
                IsValid = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Excel stream");
            throw;
        }
    }

    private string? FindBestHeaderMatch(DynamicField field, List<string> headers)
    {
        var fieldName = field.DisplayName.ToLowerInvariant();
        var fieldKey = field.FieldKey.ToLowerInvariant();
        
        // Exact match
        var exactMatch = headers.FirstOrDefault(h => h.ToLowerInvariant() == fieldName || h.ToLowerInvariant() == fieldKey);
        if (exactMatch != null) return exactMatch;
        
        // Partial match
        var partialMatch = headers.FirstOrDefault(h => 
            h.ToLowerInvariant().Contains(fieldName) || 
            fieldName.Contains(h.ToLowerInvariant()) ||
            h.ToLowerInvariant().Contains(fieldKey) ||
            fieldKey.Contains(h.ToLowerInvariant()));
        
        return partialMatch;
    }

    private double CalculateMatchConfidence(DynamicField field, string header)
    {
        var fieldName = field.DisplayName.ToLowerInvariant();
        var fieldKey = field.FieldKey.ToLowerInvariant();
        var headerLower = header.ToLowerInvariant();
        
        // Exact match = 1.0
        if (headerLower == fieldName || headerLower == fieldKey) return 1.0;
        
        // Partial match = 0.7-0.9
        if (headerLower.Contains(fieldName) || fieldName.Contains(headerLower) ||
            headerLower.Contains(fieldKey) || fieldKey.Contains(headerLower))
        {
            return 0.8;
        }
        
        // Fuzzy match = 0.3-0.6
        var similarity = CalculateStringSimilarity(fieldName, headerLower);
        return similarity > 0.5 ? similarity : 0.0;
    }

    private double CalculateStringSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
        
        var longer = s1.Length > s2.Length ? s1 : s2;
        var shorter = s1.Length > s2.Length ? s2 : s1;
        
        if (longer.Length == 0) return 1.0;
        
        var distance = LevenshteinDistance(longer, shorter);
        return (longer.Length - distance) / (double)longer.Length;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;
        
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }
        
        return matrix[s1.Length, s2.Length];
    }

    private List<Dictionary<string, object>> GenerateSampleData(List<DynamicField> fields, int rowCount)
    {
        var sampleData = new List<Dictionary<string, object>>();
        
        for (int i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object>();
            
            foreach (var field in fields)
            {
                row[field.FieldKey] = GenerateSampleValue(field, i);
            }
            
            sampleData.Add(row);
        }
        
        return sampleData;
    }

    private object GenerateSampleValue(DynamicField field, int index)
    {
        return field.FieldType.ToLowerInvariant() switch
        {
            "text" => $"Sample Text {index + 1}",
            "email" => $"sample{index + 1}@example.com",
            "number" => (index + 1) * 100,
            "date" => DateTime.Now.AddDays(index).ToString("yyyy-MM-dd"),
            "phone" => $"+1-555-{1000 + index:D4}",
            "dropdown" => $"Option {index % 3 + 1}",
            _ => $"Sample {field.FieldType} {index + 1}"
        };
    }

    private byte[] CreateExcelTemplate(List<string> headers, List<Dictionary<string, object>> sampleData)
    {
        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;
        
        var workbook = application.Workbooks.Create(1);
        var worksheet = workbook.Worksheets[0];
        
        // Add headers
        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Range[1, i + 1].Text = headers[i];
            worksheet.Range[1, i + 1].CellStyle.Font.Bold = true;
        }
        
        // Add sample data
        for (int row = 0; row < sampleData.Count; row++)
        {
            var dataRow = sampleData[row];
            int col = 1;
            
            foreach (var header in headers)
            {
                if (dataRow.ContainsKey(header))
                {
                    worksheet.Range[row + 2, col].Text = dataRow[header]?.ToString() ?? string.Empty;
                }
                col++;
            }
        }
        
        // Auto-fit columns
        worksheet.UsedRange.AutofitColumns();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private Dictionary<string, object> AnalyzeColumnTypes(List<Dictionary<string, object>> rows)
    {
        var columnTypes = new Dictionary<string, object>();
        
        if (rows.Count == 0) return columnTypes;
        
        var firstRow = rows[0];
        
        foreach (var column in firstRow.Keys)
        {
            var values = rows.Select(r => r.GetValueOrDefault(column)?.ToString() ?? string.Empty).ToList();
            columnTypes[column] = AnalyzeColumnType(values);
        }
        
        return columnTypes;
    }

    private string AnalyzeColumnType(List<string> values)
    {
        if (values.All(v => string.IsNullOrEmpty(v))) return "empty";
        
        // Check for numbers
        if (values.All(v => double.TryParse(v, out _))) return "number";
        
        // Check for dates
        if (values.All(v => DateTime.TryParse(v, out _))) return "date";
        
        // Check for emails
        if (values.All(v => v.Contains("@") && v.Contains("."))) return "email";
        
        // Check for phone numbers
        if (values.All(v => System.Text.RegularExpressions.Regex.IsMatch(v, @"^[\+]?[1-9][\d]{0,15}$"))) return "phone";
        
        return "text";
    }

    private int CountEmptyCells(List<Dictionary<string, object>> rows)
    {
        return rows.Sum(row => row.Values.Count(v => v == null || string.IsNullOrEmpty(v.ToString())));
    }

    private int CountDuplicateRows(List<Dictionary<string, object>> rows)
    {
        var rowHashes = rows.Select(row => string.Join("|", row.Values.Select(v => v?.ToString() ?? ""))).ToList();
        return rowHashes.Count - rowHashes.Distinct().Count();
    }

    // Missing methods that are called by the controller
    public async Task<ExcelUploadDto> UploadExcelAsync(UploadExcelRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("🚀 [EXCEL-UPLOAD] Starting Excel upload process for user {UserId}", userId);
            _logger.LogInformation("📋 [EXCEL-UPLOAD] Request details: FileName={FileName}, FileSize={FileSize}, LetterTypeId={LetterTypeId}", 
                request.File?.FileName, request.File?.Length, request.LetterTypeDefinitionId);

            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogError("❌ [EXCEL-UPLOAD] Excel file is null or empty");
                throw new ArgumentException("Excel file is required");
            }

            _logger.LogInformation("✅ [EXCEL-UPLOAD] File validation starting...");
            if (!await ValidateExcelFileAsync(request.File))
            {
                _logger.LogError("❌ [EXCEL-UPLOAD] File validation failed for {FileName}", request.File.FileName);
                throw new ArgumentException("Invalid Excel file format or size");
            }
            _logger.LogInformation("✅ [EXCEL-UPLOAD] File validation passed");

            // Get the letter type to determine the module
            _logger.LogInformation("🔍 [EXCEL-UPLOAD] Looking up letter type {LetterTypeId}...", request.LetterTypeDefinitionId);
            var letterType = await _letterTypeRepository.GetByIdAsync(request.LetterTypeDefinitionId);
            if (letterType == null)
            {
                _logger.LogError("❌ [EXCEL-UPLOAD] Letter type not found: {LetterTypeId}", request.LetterTypeDefinitionId);
                throw new ArgumentException("Letter type not found");
            }
            _logger.LogInformation("✅ [EXCEL-UPLOAD] Letter type found: {DisplayName}", letterType.DisplayName);

            // Upload file first
            _logger.LogInformation("📤 [EXCEL-UPLOAD] Uploading file to FileManagementService...");
            var uploadFileRequest = new UploadFileRequest
            {
                File = request.File,
                Category = "excel",
                SubCategory = "upload",
            };

            var fileReference = await _fileManagementService.UploadFileAsync(uploadFileRequest, userId);
            _logger.LogInformation("✅ [EXCEL-UPLOAD] File uploaded successfully. FileId: {FileId}", fileReference.Id);

            // Create Excel upload record
            _logger.LogInformation("📝 [EXCEL-UPLOAD] Creating ExcelUpload record...");
            
            // Validate user exists, fallback to admin if not
            var validUserId = await ValidateUserIdAsync(userId);
            
            var excelUpload = new ExcelUpload
            {
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                FileId = fileReference.Id,
                Metadata = JsonSerializer.Serialize(new { 
                    originalFileName = request.File.FileName,
                    fileSize = request.File.Length,
                    contentType = request.File.ContentType
                }),
                IsProcessed = false,
                ProcessedBy = validUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("💾 [EXCEL-UPLOAD] Saving ExcelUpload to database...");
            await _excelRepository.AddAsync(excelUpload);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ [EXCEL-UPLOAD] ExcelUpload saved successfully. UploadId: {UploadId}", excelUpload.Id);

            _logger.LogInformation("🎉 [EXCEL-UPLOAD] Excel file uploaded successfully. Upload ID: {UploadId}, User: {UserId}", 
                excelUpload.Id, userId);

            return MapToExcelUploadDto(excelUpload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-UPLOAD] Error uploading Excel file: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<ExcelProcessingResultDto> ProcessExcelAsync(Guid uploadId, ProcessExcelRequest request, string userId)
    {
        try
        {
            request.UploadId = uploadId; // Ensure consistency
            var upload = await ProcessExcelFileAsync(request, userId);
            
            return new ExcelProcessingResultDto
            {
                UploadId = upload.Id,
                IsSuccess = upload.IsProcessed,
                Message = upload.IsProcessed ? "Excel file processed successfully" : "Processing failed",
                ProcessedRows = upload.ProcessedRows,
                TotalRows = upload.ProcessedRows, // Simplified for now
                Results = upload.Results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file {UploadId}", uploadId);
            return new ExcelProcessingResultDto
            {
                UploadId = uploadId,
                IsSuccess = false,
                Message = ex.Message,
                ProcessedRows = 0,
                TotalRows = 0
            };
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

    public async Task DeleteExcelUploadAsync(Guid id, string userId)
    {
        try
        {
            var upload = await _excelRepository.GetByIdAsync(id);
            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            // Delete the file
            await _fileManagementService.DeleteFileAsync(upload.FileId, userId);

            // Delete the upload record
            await _excelRepository.DeleteAsync(upload);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Excel upload {Id} deleted by user {UserId}", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Excel upload {Id}", id);
            throw;
        }
    }

    public async Task<ExcelPreviewDto> PreviewExcelAsync(Guid id, int maxRows = 100)
    {
        try
        {
            var excelData = await ParseExcelDataAsync(id);
            
            return new ExcelPreviewDto
            {
                Headers = excelData.Headers,
                Rows = excelData.Rows.Take(maxRows).ToList(),
                TotalRows = excelData.TotalRows,
                PreviewRows = Math.Min(maxRows, excelData.TotalRows)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing Excel file {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetExcelHeadersAsync(Guid id)
    {
        try
        {
            var excelData = await ParseExcelDataAsync(id);
            return excelData.Headers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel headers for upload {Id}", id);
            throw;
        }
    }

    public async Task<ExcelValidationResultDto> ValidateExcelAsync(Guid id, ValidateExcelRequest request)
    {
        try
        {
            request.UploadId = id; // Ensure consistency
            var validationResult = await ValidateExcelDataAsync(request);
            var excelData = await ParseExcelDataAsync(id);
            
            return new ExcelValidationResultDto
            {
                IsValid = validationResult.IsValid,
                ValidationResult = validationResult,
                Headers = excelData.Headers,
                TotalRows = excelData.TotalRows
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel file {Id}", id);
            throw;
        }
    }

    public async Task<FieldMappingResultDto> MapFieldsAsync(Guid id, MapFieldsRequest request)
    {
        try
        {
            request.UploadId = id; // Ensure consistency
            
            if (request.AutoMap)
            {
                // Auto-suggest mappings
                var fieldMappingRequest = new FieldMappingRequest { UploadId = id };
                var autoMapping = await SuggestFieldMappingAsync(fieldMappingRequest);
                
                // This is simplified - the actual implementation would return multiple mappings
                return new FieldMappingResultDto
                {
                    Mappings = new List<FieldMappingDto> { autoMapping },
                    OverallConfidence = autoMapping.Confidence,
                    UnmappedFields = new List<string>(),
                    SuggestedMappings = new List<string>()
                };
            }
            else
            {
                // Use provided mappings
                return new FieldMappingResultDto
                {
                    Mappings = request.Mappings,
                    OverallConfidence = request.Mappings.Any() ? request.Mappings.Average(m => m.Confidence) : 0,
                    UnmappedFields = new List<string>(),
                    SuggestedMappings = new List<string>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping fields for Excel upload {Id}", id);
            throw;
        }
    }

    public async Task<ImportResultDto> ImportDataAsync(Guid id, ImportDataRequest request, string userId)
    {
        try
        {
            request.UploadId = id; // Ensure consistency
            
            if (request.ValidateOnly)
            {
                var validationRequest = new ValidateExcelRequest { UploadId = id };
                var validationResult = await ValidateExcelDataAsync(validationRequest);
                
                return new ImportResultDto
                {
                    IsSuccess = validationResult.IsValid,
                    ImportedRows = 0,
                    FailedRows = validationResult.InvalidRows,
                    Errors = validationResult.Errors,
                    Message = validationResult.IsValid ? "Validation passed" : "Validation failed"
                };
            }
            else
            {
                // Get upload details
                var upload = await _excelRepository.GetFirstIncludingAsync(
                    e => e.Id == id,
                    e => e.LetterTypeDefinition
                );

                if (upload == null)
                {
                    throw new ArgumentException("Excel upload not found");
                }

                // Convert mappings to JSON string
                var fieldMappings = JsonSerializer.Serialize(
                    request.Mappings.ToDictionary(m => m.ExcelColumn, m => m.DynamicField)
                );

                // Import data
                var success = await ImportExcelDataAsync(id, upload.LetterTypeDefinitionId, fieldMappings, userId);
                var excelData = await ParseExcelDataAsync(id);
                
                return new ImportResultDto
                {
                    IsSuccess = success,
                    ImportedRows = success ? excelData.TotalRows : 0,
                    FailedRows = success ? 0 : excelData.TotalRows,
                    Errors = new List<ValidationErrorDto>(),
                    Message = success ? "Data imported successfully" : "Import failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data from Excel upload {Id}", id);
            return new ImportResultDto
            {
                IsSuccess = false,
                ImportedRows = 0,
                FailedRows = 0,
                Errors = new List<ValidationErrorDto>(),
                Message = ex.Message
            };
        }
    }

    public async Task<Stream> DownloadExcelAsync(Guid id, string format = "xlsx")
    {
        try
        {
            var upload = await _excelRepository.GetFirstIncludingAsync(
                e => e.Id == id,
                e => e.File
            );

            if (upload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            return await _fileManagementService.DownloadFileAsync(upload.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading Excel file {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ExcelTemplateDto>> GetExcelTemplatesAsync(Guid? letterTypeId = null)
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, you might have a separate ExcelTemplate entity
            var uploads = await _excelRepository.GetIncludingAsync(
                e => e.IsProcessed && (letterTypeId == null || e.LetterTypeDefinitionId == letterTypeId),
                e => e.LetterTypeDefinition,
                e => e.File
            );

            return uploads.Select(upload => new ExcelTemplateDto
            {
                Id = upload.Id,
                Name = upload.File?.FileName ?? "Template",
                Description = $"Template from {upload.File?.FileName}",
                LetterTypeDefinitionId = upload.LetterTypeDefinitionId,
                LetterTypeName = upload.LetterTypeDefinition?.DisplayName ?? string.Empty,
                TemplateData = upload.ParsedData ?? string.Empty,
                FieldMappings = upload.FieldMappings ?? string.Empty,
                IsActive = true,
                CreatedAt = upload.CreatedAt,
                UpdatedAt = upload.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel templates");
            throw;
        }
    }

    public Task<ExcelTemplateDto> CreateExcelTemplateAsync(CreateExcelTemplateRequest request, string userId)
    {
        try
        {
            // This would create a new template based on an existing upload or from scratch
            // For now, we'll create a basic template structure
            var templateId = Guid.NewGuid();
            
            return Task.FromResult(new ExcelTemplateDto
            {
                Id = templateId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                TemplateData = request.TemplateData ?? string.Empty,
                FieldMappings = request.FieldMappings ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Excel template");
            throw;
        }
    }

    public Task<ExcelTemplateDto> UpdateExcelTemplateAsync(Guid id, UpdateExcelTemplateRequest request, string userId)
    {
        try
        {
            // This would update an existing template
            // For now, we'll return a mock updated template
            return Task.FromResult(new ExcelTemplateDto
            {
                Id = id,
                Name = request.Name ?? "Updated Template",
                Description = request.Description ?? string.Empty,
                LetterTypeDefinitionId = Guid.NewGuid(),
                TemplateData = request.TemplateData ?? string.Empty,
                FieldMappings = request.FieldMappings ?? string.Empty,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Excel template {Id}", id);
            throw;
        }
    }

    public Task DeleteExcelTemplateAsync(Guid id, string userId)
    {
        try
        {
            // This would delete an existing template
            // For now, we'll just log the operation
            _logger.LogInformation("Excel template {Id} deleted by user {UserId}", id, userId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Excel template {Id}", id);
            throw;
        }
    }

    public async Task<ExcelAnalyticsDto> GetExcelAnalyticsAsync(Guid? letterTypeId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var uploads = await _excelRepository.GetIncludingAsync(
                e => (letterTypeId == null || e.LetterTypeDefinitionId == letterTypeId) &&
                     (fromDate == null || e.CreatedAt >= fromDate) &&
                     (toDate == null || e.CreatedAt <= toDate)
            );

            var uploadsList = uploads.ToList();
            
            return new ExcelAnalyticsDto
            {
                TotalUploads = uploadsList.Count,
                ProcessedUploads = uploadsList.Count(u => u.IsProcessed),
                FailedUploads = uploadsList.Count(u => !u.IsProcessed),
                TotalRowsProcessed = uploadsList.Sum(u => u.ProcessedRows),
                AverageProcessingTime = 2.5, // Mock value
                UploadsByMonth = uploadsList
                    .GroupBy(u => u.CreatedAt.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                ProcessingStatusCounts = new Dictionary<string, int>
                {
                    ["Processed"] = uploadsList.Count(u => u.IsProcessed),
                    ["Pending"] = uploadsList.Count(u => !u.IsProcessed)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel analytics");
            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> GetDataFromDynamicTableAsync(string tableName, int skip = 0, int take = 100)
    {
        try
        {
            _logger.LogInformation("🔍 [EXCEL-SERVICE] Getting data from dynamic table {TableName}", tableName);
            return await _dynamicTableService.GetDataFromDynamicTableAsync(tableName, skip, take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EXCEL-SERVICE] Error getting data from dynamic table {TableName}", tableName);
            throw;
        }
    }

    private async Task<Guid> ValidateUserIdAsync(string userId)
    {
        try
        {
            // Check if the user exists
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == Guid.Parse(userId));
            if (userExists)
            {
                return Guid.Parse(userId);
            }
            
            // Fallback to admin user
            _logger.LogWarning("User {UserId} not found, using admin user as fallback", userId);
            var adminUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "admin@collabera.com");
            if (adminUser != null)
            {
                _logger.LogInformation("Using admin user {AdminId} as fallback", adminUser.Id);
                return adminUser.Id;
            }
            
            // If no admin user found, throw exception
            throw new InvalidOperationException("No valid user found for Excel upload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user ID: {UserId}", userId);
            throw;
        }
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
            FileSize = upload.File?.FileSize ?? 0,
            Metadata = upload.Metadata,
            ParsedData = upload.ParsedData,
            FieldMappings = upload.FieldMappings,
            ProcessingOptions = upload.ProcessingOptions,
            Results = upload.Results,
            IsProcessed = upload.IsProcessed,
            ProcessedRows = upload.ProcessedRows,
            ProcessedBy = upload.ProcessedBy,
            ProcessedByName = upload.ProcessedByUser?.FirstName + " " + upload.ProcessedByUser?.LastName ?? string.Empty,
            CreatedAt = upload.CreatedAt,
            UpdatedAt = upload.UpdatedAt
        };
    }

}