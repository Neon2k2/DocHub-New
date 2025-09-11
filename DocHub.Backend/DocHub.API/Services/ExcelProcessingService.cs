using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
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
                .FirstOrDefaultAsync(lt => lt.Id == letterTypeDefinitionId);

            if (letterType == null)
            {
                return new ExcelProcessingResult
                {
                    Success = false,
                    Message = "Letter type not found",
                    Errors = new List<string> { "Letter type definition not found" }
                };
            }

            using var stream = file.OpenReadStream();
            var data = await ExtractDataFromExcelAsync(stream);

            if (!data.Any())
            {
                return new ExcelProcessingResult
                {
                    Success = false,
                    Message = "No data found in Excel file",
                    Errors = new List<string> { "Excel file is empty or contains no valid data" }
                };
            }

            var standardFields = new List<string> { "EmployeeId", "EmployeeName", "Email", "Phone", "Department", "Position" };
            var fieldMappings = MapColumnsToFields(data.FirstOrDefault()?.Keys.ToList() ?? new List<string>(), standardFields)
                .ToDictionary(fm => fm.FieldKey, fm => fm.MappedColumn ?? string.Empty);
            var validationResult = ValidateData(data, standardFields);

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

    public async Task<ExcelDataValidationResult> ValidateExcelDataAsync(IFormFile file, List<string> requiredFields)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var data = await ExtractDataFromExcelAsync(stream);

            if (!data.Any())
            {
                return new ExcelDataValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "No data found in Excel file" }
                };
            }

            var validationResult = ValidateData(data, requiredFields);
            return validationResult;
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
            using var stream = file.OpenReadStream();
            return await ExtractDataFromExcelAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting data from Excel file");
            return new List<Dictionary<string, object>>();
        }
    }

    public async Task<ExcelFieldMappingResult> MapExcelColumnsAsync(IFormFile file, List<string> targetFields)
    {
        try
        {
            var data = await ExtractDataFromExcelAsync(file);
            
            if (!data.Any())
            {
                return new ExcelFieldMappingResult
                {
                    Success = false,
                    Mappings = new Dictionary<string, string>(),
                    UnmappedFields = targetFields.ToList()
                };
            }

            var columnNames = data.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
            var mappings = new Dictionary<string, string>();
            var unmappedFields = new List<string>();

            foreach (var field in targetFields)
            {
                var mappedColumn = columnNames.FirstOrDefault(col => 
                    col.Equals(field, StringComparison.OrdinalIgnoreCase)
                );

                if (mappedColumn != null)
                {
                    mappings[field] = mappedColumn;
                }
                else
                {
                    unmappedFields.Add(field);
                }
            }

            return new ExcelFieldMappingResult
            {
                Success = true,
                Mappings = mappings,
                UnmappedFields = unmappedFields,
                Suggestions = GenerateSuggestions(columnNames, unmappedFields)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping Excel columns");
            return new ExcelFieldMappingResult
            {
                Success = false,
                Mappings = new Dictionary<string, string>(),
                UnmappedFields = targetFields.ToList()
            };
        }
    }

    private List<string> GenerateSuggestions(List<string> columnNames, List<string> unmappedFields)
    {
        var suggestions = new List<string>();
        
        foreach (var field in unmappedFields)
        {
            var similarColumns = columnNames.Where(col => 
                col.Contains(field, StringComparison.OrdinalIgnoreCase) ||
                field.Contains(col, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            suggestions.AddRange(similarColumns);
        }

        return suggestions.Distinct().ToList();
    }

    private async Task<List<Dictionary<string, object>>> ExtractDataFromExcelAsync(Stream stream)
    {
        return await Task.Run(() =>
        {
            using var spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
            var workbookPart = spreadsheetDocument.WorkbookPart;
            var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();

            if (worksheetPart == null)
                return new List<Dictionary<string, object>>();

            var worksheet = worksheetPart.Worksheet;
            var sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData == null)
                return new List<Dictionary<string, object>>();

            var headers = new List<string>();
            var data = new List<Dictionary<string, object>>();
            var rows = sheetData.Elements<Row>().ToList();

            if (rows.Count == 0)
                return new List<Dictionary<string, object>>();

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

            return data;
        });
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

    private List<FieldMapping> MapColumnsToFields(List<string> columnNames, List<string> fields)
    {
        var mappings = new List<FieldMapping>();

        foreach (var field in fields)
        {
            var mapping = new FieldMapping
            {
                FieldId = Guid.NewGuid(),
                FieldKey = field,
                FieldName = field,
                DisplayName = field,
                IsRequired = false,
                MappedColumn = columnNames.FirstOrDefault(col => 
                    col.Equals(field, StringComparison.OrdinalIgnoreCase)
                ),
                Confidence = 1.0
            };

            mappings.Add(mapping);
        }

        return mappings;
    }

    private ExcelDataValidationResult ValidateData(List<Dictionary<string, object>> data, List<string> fields)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!data.Any())
        {
            errors.Add("No data found in Excel file");
            return new ExcelDataValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }

        var requiredFields = fields; // All fields are considered required for now
        var firstRow = data.FirstOrDefault();

        if (firstRow == null)
        {
            errors.Add("Excel file contains no data rows");
            return new ExcelDataValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }

        // Check for required fields
        foreach (var field in requiredFields)
        {
            var hasField = firstRow.ContainsKey(field);

            if (!hasField)
            {
                errors.Add($"Required field '{field}' not found in Excel file");
            }
        }

        // Validate data types and constraints
        foreach (var row in data)
        {
            foreach (var field in fields)
            {
                if (row.TryGetValue(field, out var value) && value != null)
                {
                    var validationResult = ValidateFieldValue(value, field);
                    if (!validationResult.IsValid)
                    {
                        errors.AddRange(validationResult.Errors);
                    }
                    warnings.AddRange(validationResult.Warnings);
                }
            }
        }

        return new ExcelDataValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings
        };
    }

    private ExcelDataValidationResult ValidateFieldValue(object value, string fieldName)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            errors.Add($"Field '{fieldName}' is required but is empty");
            return new ExcelDataValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = warnings
            };
        }

        var stringValue = value.ToString();

        // Validate field type based on field name
        if (fieldName.ToLower().Contains("email"))
        {
            if (!IsValidEmail(stringValue))
            {
                errors.Add($"Field '{fieldName}' contains invalid email format");
            }
        }
        else if (fieldName.ToLower().Contains("phone"))
        {
            // Basic phone validation
            if (stringValue.Length < 10)
            {
                errors.Add($"Field '{fieldName}' must be a valid phone number");
            }
        }

        // Basic length validation
        if (stringValue.Length > 500)
        {
            warnings.Add($"Field '{fieldName}' is very long ({stringValue.Length} characters)");
        }

        return new ExcelDataValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings
        };
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}