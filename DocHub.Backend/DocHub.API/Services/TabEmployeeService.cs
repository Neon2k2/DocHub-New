using Microsoft.EntityFrameworkCore;
using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.DTOs;
using DocHub.API.Services.Interfaces;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

namespace DocHub.API.Services;

public class TabEmployeeService : ITabEmployeeService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<TabEmployeeService> _logger;

    public TabEmployeeService(DocHubDbContext context, ILogger<TabEmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<TabEmployeeData>> GetEmployeesForTabAsync(Guid tabId, int page = 1, int limit = 50, string? search = null, string? department = null, string? status = null)
    {
        var query = _context.TabEmployeeData
            .Where(e => e.TabId == tabId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e => 
                e.EmployeeName.ToLower().Contains(searchLower) ||
                e.EmployeeId.ToLower().Contains(searchLower) ||
                (e.Email != null && e.Email.ToLower().Contains(searchLower)) ||
                (e.Department != null && e.Department.ToLower().Contains(searchLower))
            );
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(e => e.Department != null && e.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(status))
        {
            var isActive = status.ToLower() == "active";
            query = query.Where(e => e.IsActive == isActive);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / limit);

        var employees = await query
            .OrderBy(e => e.EmployeeName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return new PagedResult<TabEmployeeData>
        {
            Items = employees,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = limit
        };
    }

    public async Task<TabEmployeeData?> GetEmployeeByIdAsync(Guid id)
    {
        return await _context.TabEmployeeData
            .Include(e => e.Tab)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<TabEmployeeData> CreateEmployeeAsync(TabEmployeeData employee)
    {
        _context.TabEmployeeData.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<TabEmployeeData> UpdateEmployeeAsync(TabEmployeeData employee)
    {
        _context.TabEmployeeData.Update(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<bool> DeleteEmployeeAsync(Guid id)
    {
        var employee = await _context.TabEmployeeData.FindAsync(id);
        if (employee == null) return false;

        _context.TabEmployeeData.Remove(employee);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ImportFromExcelAsync(Guid tabId, IFormFile excelFile, Dictionary<string, string> columnMappings)
    {
        try
        {
            // Clear existing data for this tab
            await ClearTabDataAsync(tabId);

            // Parse Excel file
            var parseResult = await ParseExcelFileAsync(excelFile);
            if (!parseResult.Success)
            {
                _logger.LogError("Failed to parse Excel file for tab {TabId}: {Message}", tabId, parseResult.Message);
                return false;
            }

            // Create employee records from Excel data
            var employees = new List<TabEmployeeData>();
            foreach (var row in parseResult.Data)
            {
                var employee = new TabEmployeeData
                {
                    Id = Guid.NewGuid(),
                    TabId = tabId,
                    EmployeeId = GetValueFromRow(row, columnMappings, "EmployeeId", "EMP ID", "ID"),
                    EmployeeName = GetValueFromRow(row, columnMappings, "EmployeeName", "EMP NAME", "NAME", "Employee Name"),
                    Email = GetValueFromRow(row, columnMappings, "Email", "EMAIL", "Email Address"),
                    Phone = GetValueFromRow(row, columnMappings, "Phone", "PHONE", "Phone Number"),
                    Department = GetValueFromRow(row, columnMappings, "Department", "DEPT", "Department"),
                    Position = GetValueFromRow(row, columnMappings, "Position", "POSITION", "Job Title", "Title"),
                    DataSource = "Excel",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Store custom fields as JSON
                var customFields = new Dictionary<string, object>();
                foreach (var kvp in row)
                {
                    if (!IsStandardField(kvp.Key))
                    {
                        customFields[kvp.Key] = kvp.Value;
                    }
                }
                employee.CustomFields = customFields.Count > 0 ? JsonSerializer.Serialize(customFields) : null;

                employees.Add(employee);
            }

            // Save to database
            _context.TabEmployeeData.AddRange(employees);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully imported {Count} employees from Excel for tab {TabId}", employees.Count, tabId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Excel data for tab {TabId}", tabId);
            return false;
        }
    }

    private async Task<ExcelParseResult> ParseExcelFileAsync(IFormFile file)
    {
        try
        {
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            using var spreadsheetDocument = SpreadsheetDocument.Open(tempPath, false);
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

            // Clean up temp file
            File.Delete(tempPath);

            return new ExcelParseResult
            {
                Success = true,
                Headers = headers,
                Data = data,
                RowsProcessed = data.Count,
                Message = "Excel file parsed successfully"
            };
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

    private string? GetValueFromRow(Dictionary<string, object> row, Dictionary<string, string> columnMappings, params string[] possibleKeys)
    {
        // First try to find a direct mapping
        foreach (var key in possibleKeys)
        {
            if (row.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString();
            }
        }

        // Then try to find through column mappings - look for Excel headers that map to our target field
        foreach (var key in possibleKeys)
        {
            var excelHeader = columnMappings.FirstOrDefault(kvp => 
                string.Equals(kvp.Value, key, StringComparison.OrdinalIgnoreCase)).Key;
            
            if (!string.IsNullOrEmpty(excelHeader) && row.TryGetValue(excelHeader, out var value) && value != null)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private bool IsStandardField(string fieldName)
    {
        var standardFields = new[] { "EmployeeId", "EMP ID", "ID", "EmployeeName", "EMP NAME", "NAME", "Employee Name", 
                                   "Email", "EMAIL", "Email Address", "Phone", "PHONE", "Phone Number", 
                                   "Department", "DEPT", "Department", "Position", "POSITION", "Job Title", "Title" };
        return standardFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<TabEmployeeData>> GetEmployeesByTabIdAsync(Guid tabId)
    {
        return await _context.TabEmployeeData
            .Where(e => e.TabId == tabId && e.IsActive)
            .OrderBy(e => e.EmployeeName)
            .ToListAsync();
    }

    public async Task<bool> ClearTabDataAsync(Guid tabId)
    {
        try
        {
            var employees = await _context.TabEmployeeData
                .Where(e => e.TabId == tabId)
                .ToListAsync();

            _context.TabEmployeeData.RemoveRange(employees);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data for tab {TabId}", tabId);
            return false;
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
