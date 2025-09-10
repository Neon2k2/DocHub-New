using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace DocHub.API.Services;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<ExcelProcessingService> _logger;

    public ExcelProcessingService(DocHubDbContext context, ILogger<ExcelProcessingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExcelProcessingResult> ProcessExcelFileAsync(IFormFile file, Guid letterTypeDefinitionId)
    {
        try
        {
            var letterType = await _context.LetterTypeDefinitions
                .Include(lt => lt.Fields)
                .FirstOrDefaultAsync(lt => lt.Id == letterTypeDefinitionId);

            if (letterType == null)
            {
                return new ExcelProcessingResult
                {
                    Success = false,
                    Message = "Letter type definition not found",
                    Errors = new List<string> { "Invalid letter type definition ID" }
                };
            }

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new ExcelProcessingResult
                {
                    Success = false,
                    Message = "No worksheets found in Excel file",
                    Errors = new List<string> { "Excel file contains no worksheets" }
                };
            }

            var data = ExtractDataFromWorksheet(worksheet);
            var fieldMappings = MapColumnsToFields(data.FirstOrDefault()?.Keys.ToList() ?? new List<string>(), letterType.Fields.ToList());
            var validationResult = ValidateData(data, letterType.Fields.ToList());

            return new ExcelProcessingResult
            {
                Success = validationResult.IsValid,
                Message = validationResult.IsValid ? "Excel file processed successfully" : "Excel file processed with validation errors",
                Data = data,
                FieldMappings = fieldMappings,
                Errors = validationResult.Errors,
                Warnings = validationResult.Warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file for letter type {LetterTypeId}", letterTypeDefinitionId);
            return new ExcelProcessingResult
            {
                Success = false,
                Message = "Error processing Excel file",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ExcelDataValidationResult> ValidateExcelDataAsync(IFormFile file, List<DynamicField> requiredFields)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new ExcelDataValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "No worksheets found in Excel file" }
                };
            }

            var data = ExtractDataFromWorksheet(worksheet);
            return ValidateData(data, requiredFields);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel data");
            return new ExcelDataValidationResult
            {
                IsValid = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<List<Dictionary<string, object>>> ExtractDataFromExcelAsync(IFormFile file)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new List<Dictionary<string, object>>();
            }

            return ExtractDataFromWorksheet(worksheet);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting data from Excel file");
            return new List<Dictionary<string, object>>();
        }
    }

    public async Task<ExcelFieldMappingResult> MapExcelColumnsAsync(IFormFile file, List<DynamicField> targetFields)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new ExcelFieldMappingResult
                {
                    Success = false,
                    UnmappedFields = targetFields.Select(f => f.FieldName).ToList()
                };
            }

            var excelColumns = GetExcelColumns(worksheet);
            var mappings = new Dictionary<string, string>();
            var unmappedFields = new List<string>();
            var suggestions = new List<string>();

            foreach (var field in targetFields)
            {
                var mappedColumn = FindBestMatch(excelColumns, field.FieldName);
                if (mappedColumn != null)
                {
                    mappings[field.FieldName] = mappedColumn;
                }
                else
                {
                    unmappedFields.Add(field.FieldName);
                    suggestions.AddRange(GetSuggestions(excelColumns, field.FieldName));
                }
            }

            return new ExcelFieldMappingResult
            {
                Success = unmappedFields.Count == 0,
                Mappings = mappings,
                UnmappedFields = unmappedFields,
                Suggestions = suggestions.Distinct().ToList()
            };
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping Excel columns");
            return new ExcelFieldMappingResult
            {
                Success = false,
                UnmappedFields = targetFields.Select(f => f.FieldName).ToList()
            };
        }
    }

    private List<Dictionary<string, object>> ExtractDataFromWorksheet(ExcelWorksheet worksheet)
    {
        var data = new List<Dictionary<string, object>>();
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var colCount = worksheet.Dimension?.Columns ?? 0;

        if (rowCount <= 1) return data; // No data rows

        // Get headers from first row
        var headers = new List<string>();
        for (int col = 1; col <= colCount; col++)
        {
            var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(header))
            {
                headers.Add(header);
            }
        }

        // Extract data rows
        for (int row = 2; row <= rowCount; row++)
        {
            var rowData = new Dictionary<string, object>();
            for (int col = 1; col <= headers.Count; col++)
            {
                var value = worksheet.Cells[row, col].Value;
                rowData[headers[col - 1]] = value ?? string.Empty;
            }
            data.Add(rowData);
        }

        return data;
    }

    private List<string> GetExcelColumns(ExcelWorksheet worksheet)
    {
        var columns = new List<string>();
        var colCount = worksheet.Dimension?.Columns ?? 0;

        for (int col = 1; col <= colCount; col++)
        {
            var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(header))
            {
                columns.Add(header);
            }
        }

        return columns;
    }

    private ExcelDataValidationResult ValidateData(List<Dictionary<string, object>> data, List<DynamicField> requiredFields)
    {
        var result = new ExcelDataValidationResult
        {
            IsValid = true,
            ValidRows = data.Count,
            InvalidRows = 0
        };

        foreach (var row in data)
        {
            var rowValid = true;
            foreach (var field in requiredFields)
            {
                if (field.IsRequired && (!row.ContainsKey(field.FieldName) || string.IsNullOrEmpty(row[field.FieldName]?.ToString())))
                {
                    result.Errors.Add($"Row {data.IndexOf(row) + 2}: Missing required field '{field.FieldName}'");
                    rowValid = false;
                }
            }

            if (!rowValid)
            {
                result.InvalidRows++;
                result.ValidRows--;
            }
        }

        result.IsValid = result.InvalidRows == 0;
        return result;
    }

    private Dictionary<string, string> MapColumnsToFields(List<string> excelColumns, List<DynamicField> targetFields)
    {
        var mappings = new Dictionary<string, string>();

        foreach (var field in targetFields)
        {
            var mappedColumn = FindBestMatch(excelColumns, field.FieldName);
            if (mappedColumn != null)
            {
                mappings[field.FieldName] = mappedColumn;
            }
        }

        return mappings;
    }

    private string? FindBestMatch(List<string> excelColumns, string targetField)
    {
        // Exact match
        var exactMatch = excelColumns.FirstOrDefault(c => 
            string.Equals(c, targetField, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null) return exactMatch;

        // Partial match
        var partialMatch = excelColumns.FirstOrDefault(c => 
            c.Contains(targetField, StringComparison.OrdinalIgnoreCase) ||
            targetField.Contains(c, StringComparison.OrdinalIgnoreCase));
        if (partialMatch != null) return partialMatch;

        // Fuzzy match using similarity
        var bestMatch = excelColumns
            .Select(c => new { Column = c, Similarity = CalculateSimilarity(c, targetField) })
            .Where(x => x.Similarity > 0.6)
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();

        return bestMatch?.Column;
    }

    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        var longer = s1.Length > s2.Length ? s1 : s2;
        var shorter = s1.Length > s2.Length ? s2 : s1;

        if (longer.Length == 0) return 1.0;

        var distance = LevenshteinDistance(longer, shorter);
        return (longer.Length - distance) / (double)longer.Length;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

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

    private List<string> GetSuggestions(List<string> excelColumns, string targetField)
    {
        return excelColumns
            .Where(c => CalculateSimilarity(c, targetField) > 0.3)
            .OrderByDescending(c => CalculateSimilarity(c, targetField))
            .Take(3)
            .ToList();
    }
}
