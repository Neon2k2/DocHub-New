using System.IO.Compression;
using Microsoft.AspNetCore.Hosting;
using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using Syncfusion.DocIO;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DocHub.Shared.DTOs.Tabs;
using DocHub.Shared.DTOs.Common;
using System.Data.Common;
using DocHub.Infrastructure.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Pictures;
using SystemPath = System.IO.Path;

namespace DocHub.Application.Services;

public class DynamicLetterGenerationService : IDynamicLetterGenerationService
{
    private readonly IDbContext _dbContext;
    private readonly ISignatureCleanupService _signatureCleanupService;
    private readonly ILogger<DynamicLetterGenerationService> _logger;
    private readonly IRepository<TableSchema> _tableSchemaRepository;

    public DynamicLetterGenerationService(
        IDbContext dbContext,
        ISignatureCleanupService signatureCleanupService,
        ILogger<DynamicLetterGenerationService> logger,
        IRepository<TableSchema> tableSchemaRepository)
    {
        _dbContext = dbContext;
        _signatureCleanupService = signatureCleanupService;
        _logger = logger;
        _tableSchemaRepository = tableSchemaRepository;
    }

    public async Task<byte[]> GenerateLetterAsync(DynamicTabDto tab, EmployeeDto employee, DocumentTemplate template, string? signaturePath = null, Dictionary<string, object>? employeeData = null)
    {
        try
        {
            _logger.LogInformation("Starting letter generation for tab: {TabId}, employee: {EmployeeId}", tab.Id, employee.Id);

            // Get the dynamic fields for this tab from the tab configuration
            var dynamicFields = GetDynamicFieldsFromTab(tab);
            
            // Get employee data from dynamic table or use provided data
            Dictionary<string, object> employeeDataDict;
            if (employeeData != null && employeeData.Any())
            {
                _logger.LogInformation("Using provided employee data for employee: {EmployeeId} with {Count} fields", employee.EmployeeId, employeeData.Count);
                employeeDataDict = employeeData;
            }
            else
            {
                _logger.LogInformation("No provided employee data, fetching from dynamic table for employee: {EmployeeId}", employee.EmployeeId);
                employeeDataDict = await GetEmployeeDataFromDynamicTable(employee.EmployeeId, tab);
            }
            
            // Create placeholder mapping
            var placeholderMap = CreatePlaceholderMapping(employee, employeeDataDict, dynamicFields);

            _logger.LogInformation("Generated placeholder map with {Count} entries", placeholderMap.Count);

            // Read template file
            var templateBytes = await ReadTemplateFile(template);
            
            // Process the template
            var letterBytes = await ProcessTemplate(templateBytes, placeholderMap, signaturePath);

            _logger.LogInformation("Letter generated successfully for employee: {EmployeeId}", employee.Id);
            return letterBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating letter for tab: {TabId}, employee: {EmployeeId}", tab.Id, employee.Id);
            throw;
        }
    }

    public async Task<byte[]> GenerateLetterZipAsync(DynamicTabDto tab, List<(EmployeeDto employee, DocumentTemplate template)> employees, string? signaturePath = null)
    {
        try
        {
            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var (employee, template) in employees)
                {
                    try
                    {
                        var letterBytes = await GenerateLetterAsync(tab, employee, template, signaturePath, null);
                        var fileName = $"{employee.EmployeeId}_{tab.Name.Replace(" ", "_")}.docx";
                        var entry = archive.CreateEntry(fileName);

                        using var entryStream = entry.Open();
                        entryStream.Write(letterBytes, 0, letterBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating letter for employee: {EmployeeId}", employee.Id);
                        // Continue with other employees
                    }
                }
            }

            zipStream.Position = 0;
            _logger.LogInformation("ZIP file created successfully for {Count} employees", employees.Count);
            return zipStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating letter ZIP for tab: {TabId}", tab.Id);
            throw;
        }
    }

    public async Task<byte[]?> GeneratePdfPreviewAsync(DynamicTabDto tab, EmployeeDto employee, DocumentTemplate template, string? signaturePath = null, Dictionary<string, object>? employeeData = null)
    {
        try
        {
            var docxBytes = await GenerateLetterAsync(tab, employee, template, signaturePath, employeeData);
            var pdfBytes = ConvertDocxToPdf(docxBytes);

            _logger.LogInformation("PDF preview generated successfully for employee: {EmployeeId}", employee.Id);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF preview for employee: {EmployeeId}", employee.Id);
            return null;
        }
    }

    public byte[] ConvertDocxToPdf(byte[] docxBytes)
    {
        try
        {
            using var stream = new MemoryStream(docxBytes);
            var wordDocument = new WordDocument(stream, Syncfusion.DocIO.FormatType.Docx);

            using var renderer = new DocIORenderer();
            using var pdfDocument = renderer.ConvertToPDF(wordDocument);
            using var pdfStream = new MemoryStream();

            pdfDocument.Save(pdfStream);
            pdfDocument.Close();

            _logger.LogInformation("DOCX to PDF conversion successful");
            return pdfStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DOCX to PDF conversion");
            throw;
        }
    }

    private List<DynamicField> GetDynamicFieldsFromTab(DynamicTabDto tab)
    {
        var fields = new List<DynamicField>();
        
        if (tab.Fields != null)
        {
            foreach (var fieldConfig in tab.Fields)
            {
                fields.Add(new DynamicField
                {
                    Id = Guid.NewGuid(), // Generate a temporary ID
                    FieldKey = fieldConfig.Name,
                    FieldName = fieldConfig.Name,
                    DisplayName = fieldConfig.DisplayName,
                    FieldType = fieldConfig.Type,
                    IsRequired = fieldConfig.Required,
                    OrderIndex = fieldConfig.Order,
                    LetterTypeDefinitionId = Guid.Parse(tab.Id),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        
        return fields;
    }

    private async Task<List<DynamicField>> GetDynamicFieldsForTab(string tabId)
    {
        var fields = await _dbContext.DynamicFields
            .Where(f => f.LetterTypeDefinitionId == Guid.Parse(tabId))
            .ToListAsync();
        return fields;
    }

    private async Task<Dictionary<string, object>> GetEmployeeDataFromDynamicTable(string employeeId, DynamicTabDto tab)
    {
        try
        {
            var employeeData = new Dictionary<string, object>();
            
            // Get the table schema for this tab
            var tableSchema = await _tableSchemaRepository.FirstOrDefaultAsync(ts => ts.LetterTypeDefinitionId == Guid.Parse(tab.Id));
            if (tableSchema == null)
            {
                _logger.LogWarning("No table schema found for tab: {TabId}", tab.Id);
                return employeeData;
            }

            // Query the dynamic table to get employee data
            var tableName = tableSchema.TableName;
            var query = $"SELECT * FROM [{tableName}] WHERE [EMP ID] = @EmployeeId";
            
            using var connection = ((DbContext)_dbContext).Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = query;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@EmployeeId";
            parameter.Value = employeeId;
            command.Parameters.Add(parameter);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    employeeData[columnName] = value ?? string.Empty;
                }
                
                _logger.LogInformation("Retrieved employee data from dynamic table for {EmployeeId}: {Count} fields", employeeId, employeeData.Count);
                _logger.LogInformation("Employee data keys: {Keys}", string.Join(", ", employeeData.Keys));
            }
            else
            {
                _logger.LogWarning("Employee {EmployeeId} not found in dynamic table {TableName}", employeeId, tableName);
            }

            return employeeData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee data from dynamic table for employee: {EmployeeId}", employeeId);
            return new Dictionary<string, object>();
        }
    }

    private Dictionary<string, string> CreatePlaceholderMapping(EmployeeDto employee, Dictionary<string, object> employeeData, List<DynamicField> fields)
    {
        var placeholderMap = new Dictionary<string, string>();

        _logger.LogInformation("Creating placeholder mapping for employee: {EmployeeId}", employee.EmployeeId);
        _logger.LogInformation("Available employee data keys: {Keys}", string.Join(", ", employeeData.Keys));
        _logger.LogInformation("Available field configurations: {Fields}", string.Join(", ", fields.Select(f => f.FieldKey)));

        // Map standard employee fields with template-expected names
        placeholderMap["EmpName"] = employee.Name ?? string.Empty;
        placeholderMap["EmpID"] = employee.EmployeeId ?? string.Empty;
        placeholderMap["Email"] = employee.Email ?? string.Empty;
        placeholderMap["FirstName"] = employee.FirstName ?? string.Empty;
        placeholderMap["LastName"] = employee.LastName ?? string.Empty;
        
        // Map common template placeholders that might be used
        // These will be overridden by dynamic field mapping below if data is available
        placeholderMap["DOJ"] = employee.HireDate?.ToString("dd/MM/yyyy") ?? string.Empty;
        placeholderMap["LWD"] = string.Empty; // Will be filled from dynamic data
        placeholderMap["CTC"] = string.Empty; // Will be filled from dynamic data
        placeholderMap["DateOfJoining"] = employee.HireDate?.ToString("dd/MM/yyyy") ?? string.Empty;
        placeholderMap["LastWorkingDay"] = string.Empty; // Will be filled from dynamic data
        placeholderMap["Salary"] = string.Empty; // Will be filled from dynamic data

        // Map dynamic fields from the tab configuration
        foreach (var field in fields)
        {
            var fieldKey = field.FieldKey ?? string.Empty;
            
            // Try to find the data using the field name first
            if (employeeData.TryGetValue(field.FieldName ?? string.Empty, out var value))
            {
                placeholderMap[fieldKey] = value?.ToString() ?? string.Empty;
                _logger.LogInformation("Mapped field {FieldKey} from {FieldName} to value: {Value}", fieldKey, field.FieldName, value);
            }
            else
            {
                // Try alternative field names
                var alternativeNames = new[]
                {
                    field.FieldName?.Trim(),
                    field.DisplayName?.Trim(),
                    field.FieldKey?.Trim()
                }.Where(n => !string.IsNullOrEmpty(n));

                bool found = false;
                foreach (var altName in alternativeNames)
                {
                    if (employeeData.TryGetValue(altName!, out var altValue))
                    {
                        placeholderMap[fieldKey] = altValue?.ToString() ?? string.Empty;
                        _logger.LogInformation("Mapped field {FieldKey} from alternative name {AltName} to value: {Value}", fieldKey, altName, altValue);
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    _logger.LogWarning("No data found for field {FieldKey} (tried: {FieldName}, {DisplayName})", fieldKey, field.FieldName, field.DisplayName);
                }
            }
        }

        // Map common field variations with template-expected names
        var commonMappings = new Dictionary<string, string[]>
        {
            ["EmpID"] = new[] { "EMP ID", "EmployeeId", "Employee_ID" },
            ["EmpName"] = new[] { "EMP NAME", "EmployeeName", "Employee_Name", "Name" },
            ["Client"] = new[] { "CLIENT", "Client", "Company" },
            ["DOJ"] = new[] { "DOJ", "DateOfJoining", "JoinDate" },
            ["LWD"] = new[] { "LWD", "LastWorkingDay", "EndDate" },
            ["Designation"] = new[] { "DESIGNATION", "Designation", "Position", "Role" },
            ["CTC"] = new[] { "CTC", "Salary", "Compensation" },
            ["Email"] = new[] { "EMAIL", "Email", "EmailAddress" }
        };

        foreach (var mapping in commonMappings)
        {
            if (!placeholderMap.ContainsKey(mapping.Key))
            {
                foreach (var fieldName in mapping.Value)
                {
                    if (employeeData.TryGetValue(fieldName, out var value))
                    {
                        placeholderMap[mapping.Key] = value?.ToString() ?? string.Empty;
                        _logger.LogInformation("Mapped common field {MappingKey} from {FieldName} to value: {Value}", mapping.Key, fieldName, value);
                        break;
                    }
                }
            }
        }

        // Add common placeholders
        placeholderMap["Date"] = DateTime.Now.ToString("dd/MM/yyyy");
        placeholderMap["CurrentDate"] = DateTime.Now.ToString("dd/MM/yyyy");
        placeholderMap["CurrentTime"] = DateTime.Now.ToString("HH:mm:ss");
        placeholderMap["CompanyName"] = "Collabera Talent Solutions Pvt. Ltd";

        _logger.LogInformation("Created placeholder mapping with {Count} entries: {Mappings}", 
            placeholderMap.Count, 
            string.Join(", ", placeholderMap.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        
        return placeholderMap;
    }

    private async Task<byte[]> ReadTemplateFile(DocumentTemplate template)
    {
        try
        {
            // Try to read from the file path if it's available
            if (template.File != null && !string.IsNullOrEmpty(template.File.FilePath) && File.Exists(template.File.FilePath))
            {
                _logger.LogInformation("Reading template from File.FilePath: {FilePath}", template.File.FilePath);
                return await File.ReadAllBytesAsync(template.File.FilePath);
            }

            // If the file reference is not loaded, try to construct the path
            var uploadsPath = SystemPath.Combine(Directory.GetCurrentDirectory(), "uploads", "template");
            _logger.LogInformation("Looking for template file in: {UploadsPath}", uploadsPath);
            
            // Try different file naming patterns
            var possibleFileNames = new[]
            {
                $"{template.FileId}.docx",
                $"{template.FileId}.doc",
                template.File?.FileName ?? $"{template.Name}.docx",
                template.File?.FileName ?? $"{template.Name}.doc"
            };

            foreach (var fileName in possibleFileNames)
            {
                var filePath = SystemPath.Combine(uploadsPath, fileName);
                _logger.LogInformation("Checking file path: {FilePath}", filePath);
                
                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Found template file at: {FilePath}", filePath);
                    return await File.ReadAllBytesAsync(filePath);
                }
            }

            // If no specific file found, try to find any .docx file in the template directory
            var templateFiles = Directory.GetFiles(uploadsPath, "*.docx");
            if (templateFiles.Length > 0)
            {
                _logger.LogInformation("No specific template found, using first available template: {FilePath}", templateFiles[0]);
                return await File.ReadAllBytesAsync(templateFiles[0]);
            }

            throw new FileNotFoundException($"Template file not found for template: {template.Id}. Checked paths: {string.Join(", ", possibleFileNames.Select(f => SystemPath.Combine(uploadsPath, f)))}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading template file: {TemplateId}", template.Id);
            throw;
        }
    }

    private async Task<byte[]> ProcessTemplate(byte[] templateBytes, Dictionary<string, string> placeholderMap, string? signaturePath = null)
    {
        try
        {
            using var templateStream = new MemoryStream(templateBytes);
            using var wordDoc = new WordDocument(templateStream, FormatType.Docx);
            using var outputStream = new MemoryStream();

            _logger.LogInformation("Processing Word document with Syncfusion DocIO - filling Rich Content Controls");

            // Fill Rich Content Controls instead of replacing text
            FillRichContentControls(wordDoc, placeholderMap);

            // Handle signature replacement separately
            if (!string.IsNullOrEmpty(signaturePath))
            {
                // Convert to OpenXML for signature processing
                using var docxStream = new MemoryStream();
                wordDoc.Save(docxStream, Syncfusion.DocIO.FormatType.Docx);
                docxStream.Position = 0;

                // Process signature using OpenXML
                await ProcessSignaturePlaceholderAsync(docxStream, signaturePath);

                // Create new WordDocument from the updated stream
                docxStream.Position = 0;
                using var updatedWordDoc = new WordDocument(docxStream, Syncfusion.DocIO.FormatType.Docx);
                
                // Save the final document
                updatedWordDoc.Save(outputStream, FormatType.Docx);
            }
            else
            {
                // Save the document without signature processing
                wordDoc.Save(outputStream, FormatType.Docx);
            }

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template with Syncfusion");
            throw;
        }
    }

    private void FillRichContentControls(WordDocument wordDoc, Dictionary<string, string> placeholderMap)
    {
        try
        {
            _logger.LogInformation("=== STARTING RICH TEXT CONTENT CONTROL PROCESSING ===");
            _logger.LogInformation("Available placeholder data: {Placeholders}", string.Join(", ", placeholderMap.Keys));

            int totalContentControls = 0;
            int filledControls = 0;

            // Method 1: Iterate through all sections and paragraphs
            _logger.LogInformation("=== METHOD 1: Iterating through sections and paragraphs ===");
            foreach (IWSection section in wordDoc.Sections)
            {
                _logger.LogInformation("Processing section {SectionIndex}", wordDoc.Sections.IndexOf(section));
                
                foreach (IWParagraph paragraph in section.Paragraphs)
                {
                    _logger.LogInformation("Processing paragraph {ParagraphIndex} in section", section.Paragraphs.IndexOf(paragraph));
                    
                    foreach (ParagraphItem item in paragraph.ChildEntities)
                    {
                        _logger.LogInformation("Found paragraph item of type: {ItemType}", item.GetType().Name);
                        
                        // Check for different types of content controls
                        if (item is InlineContentControl inlineControl)
                        {
                            totalContentControls++;
                            _logger.LogInformation("Found InlineContentControl");
                            
                            // Only process rich text content controls, skip picture content controls
                            var tagName = inlineControl.ContentControlProperties?.Tag ?? string.Empty;
                            if (tagName.Trim() != "Signature") // Skip signature content controls as they are handled separately
                            {
                                ProcessInlineContentControl(inlineControl, placeholderMap, ref filledControls);
                            }
                            else
                            {
                                _logger.LogInformation("Skipping Signature content control - will be handled by picture content control processor");
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Item is not a content control: {ItemType}", item.GetType().Name);
                        }
                    }
                }
            }

            _logger.LogInformation("=== RICH TEXT CONTENT CONTROL PROCESSING COMPLETE ===");
            _logger.LogInformation("Total content controls found: {TotalCount}", totalContentControls);
            _logger.LogInformation("Successfully filled: {FilledCount}", filledControls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filling Rich Content Controls");
            throw;
        }
    }


    private void ProcessInlineContentControl(InlineContentControl inlineControl, Dictionary<string, string> placeholderMap, ref int filledControls)
    {
        try
        {
            var tagName = inlineControl.ContentControlProperties?.Tag ?? string.Empty;
            var title = inlineControl.ContentControlProperties?.Title ?? string.Empty;
            
            _logger.LogInformation("Processing InlineContentControl - Tag: '{TagName}', Title: '{Title}'", tagName, title);

            // Try to find matching data
                                string? valueToFill = null;
            string matchedKey = string.Empty;
                                
                                // Try exact match first
                                if (placeholderMap.TryGetValue(tagName, out valueToFill))
                                {
                matchedKey = tagName;
                                    _logger.LogInformation("Found exact match for tag {TagName}: {Value}", tagName, valueToFill);
                                }
                                else
                                {
                                    // Try case-insensitive match
                                    var caseInsensitiveMatch = placeholderMap.FirstOrDefault(kvp => 
                                        string.Equals(kvp.Key, tagName, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (!string.IsNullOrEmpty(caseInsensitiveMatch.Key))
                                    {
                                        valueToFill = caseInsensitiveMatch.Value;
                    matchedKey = caseInsensitiveMatch.Key;
                                        _logger.LogInformation("Found case-insensitive match for tag {TagName}: {Value}", tagName, valueToFill);
                                    }
                                }

                                if (!string.IsNullOrEmpty(valueToFill))
                                {
                                    // Clear existing content and add new text
                                    inlineControl.ParagraphItems.Clear();
                var textRange = new WTextRange(inlineControl.Document);
                                    textRange.Text = valueToFill;
                                    inlineControl.ParagraphItems.Add(textRange);
                                    
                _logger.LogInformation("Successfully filled InlineContentControl {TagName} with value: {Value}", tagName, valueToFill);
                filledControls++;
                                }
                                else
                                {
                _logger.LogWarning("No matching data found for InlineContentControl tag: '{TagName}'. Available keys: {AvailableKeys}", 
                                        tagName, string.Join(", ", placeholderMap.Keys));
                                }
                            }
                            catch (Exception ex)
                            {
            _logger.LogError(ex, "Error processing InlineContentControl with tag: {TagName}", inlineControl.ContentControlProperties?.Tag);
        }
    }

    private void ProcessTableForContentControls(WTable table, Dictionary<string, string> placeholderMap, ref int totalContentControls, ref int filledControls)
    {
        try
        {
            _logger.LogInformation("Processing table with {RowCount} rows", table.Rows.Count);
            
            foreach (WTableRow row in table.Rows)
            {
                foreach (WTableCell cell in row.Cells)
                {
                    foreach (IWParagraph paragraph in cell.Paragraphs)
                    {
                        foreach (ParagraphItem item in paragraph.ChildEntities)
                        {
                            if (item is InlineContentControl inlineControl)
                            {
                                totalContentControls++;
                                _logger.LogInformation("Found InlineContentControl in table cell");
                                ProcessInlineContentControl(inlineControl, placeholderMap, ref filledControls);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing table for content controls");
        }
    }

    private async Task ProcessSignaturePlaceholderAsync(MemoryStream docxStream, string signaturePath)
    {
        try
        {
            _logger.LogInformation("=== STARTING SIGNATURE PROCESSING ===");
            _logger.LogInformation("Processing signature placeholder with path: {SignaturePath}", signaturePath);

            string fullSignaturePath = string.Empty;
            byte[]? signatureBytes = null;
            string signatureFileName = string.Empty;

            // Method 1: Try to look up the FileReference from the database
            try
            {
                fullSignaturePath = await GetSignatureFilePath(signaturePath);
                if (!string.IsNullOrEmpty(fullSignaturePath) && File.Exists(fullSignaturePath))
                {
                    _logger.LogInformation("Found signature file via database lookup: {FullSignaturePath}", fullSignaturePath);
                    signatureBytes = File.ReadAllBytes(fullSignaturePath);
                    signatureFileName = SystemPath.GetFileName(fullSignaturePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database lookup failed for signature file, trying fallback methods");
            }

            // Method 2: If database lookup failed, try to use the signaturePath directly as a file path
            if (signatureBytes == null && !string.IsNullOrEmpty(signaturePath))
            {
                // Check if signaturePath is already a full file path
                if (File.Exists(signaturePath))
                {
                    _logger.LogInformation("Using signaturePath directly as file path: {SignaturePath}", signaturePath);
                    signatureBytes = File.ReadAllBytes(signaturePath);
                    signatureFileName = SystemPath.GetFileName(signaturePath);
                    fullSignaturePath = signaturePath;
                }
                else
                {
                    // Try common signature file locations
                    var possiblePaths = new[]
                    {
                        SystemPath.Combine("uploads", "signature", signaturePath),
                        SystemPath.Combine("uploads", "signature", $"{signaturePath}.jpg"),
                        SystemPath.Combine("uploads", "signature", $"{signaturePath}.jpeg"),
                        SystemPath.Combine("uploads", "signature", $"{signaturePath}.png"),
                        SystemPath.Combine("uploads", "signature", $"{signaturePath}.gif"),
                        SystemPath.Combine("uploads", "signature", $"{signaturePath}.bmp")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            _logger.LogInformation("Found signature file in common location: {Path}", path);
                            signatureBytes = File.ReadAllBytes(path);
                            signatureFileName = SystemPath.GetFileName(path);
                            fullSignaturePath = path;
                            break;
                        }
                    }
                }
            }

            // Method 3: Use fallback signature if available
            if (signatureBytes == null)
            {
                var fallbackPath = GetFallbackSignaturePath();
                if (!string.IsNullOrEmpty(fallbackPath) && File.Exists(fallbackPath))
                {
                    _logger.LogInformation("Using fallback signature file: {FallbackPath}", fallbackPath);
                    signatureBytes = File.ReadAllBytes(fallbackPath);
                    signatureFileName = SystemPath.GetFileName(fallbackPath);
                    fullSignaturePath = fallbackPath;
                }
            }

            // Method 4: Try to find the specific signature file by matching the signaturePath with available files
            if (signatureBytes == null)
            {
                var signatureDir = SystemPath.Combine("uploads", "signature");
                if (Directory.Exists(signatureDir))
                {
                    _logger.LogInformation("Searching for signature file matching: {SignaturePath}", signaturePath);
                    
                    // First, try to find exact match by filename (without extension)
                    var signatureFiles = Directory.GetFiles(signatureDir, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => SystemPath.GetExtension(f).ToLower() == ".jpg" || 
                                   SystemPath.GetExtension(f).ToLower() == ".jpeg" || 
                                   SystemPath.GetExtension(f).ToLower() == ".png" ||
                                   SystemPath.GetExtension(f).ToLower() == ".gif" ||
                                   SystemPath.GetExtension(f).ToLower() == ".bmp")
                        .ToArray();

                    _logger.LogInformation("Found {Count} signature files in directory", signatureFiles.Length);
                    foreach (var file in signatureFiles)
                    {
                        _logger.LogInformation("Available signature file: {FileName}", SystemPath.GetFileName(file));
                    }

                    // Try to find a file that matches the signaturePath (could be filename or ID)
                    var matchingFile = signatureFiles.FirstOrDefault(f => 
                    {
                        var fileName = SystemPath.GetFileNameWithoutExtension(f);
                        var fileExtension = SystemPath.GetExtension(f);
                        var fullFileName = SystemPath.GetFileName(f);
                        
                        _logger.LogInformation("Checking file: {FileName} against signaturePath: {SignaturePath}", fileName, signaturePath);
                        
                        // Check for exact matches first
                        if (fileName.Equals(signaturePath, StringComparison.OrdinalIgnoreCase) ||
                            fullFileName.Equals(signaturePath, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Exact match found: {FileName}", fileName);
                            return true;
                        }
                        
                        // Check if signaturePath is a GUID and matches the filename (without extension)
                        if (Guid.TryParse(signaturePath, out var guid) && fileName.Equals(guid.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("GUID match found: {FileName} matches GUID {Guid}", fileName, guid);
                            return true;
                        }
                        
                        // Check for partial matches (contains)
                        if (fileName.Contains(signaturePath, StringComparison.OrdinalIgnoreCase) ||
                            signaturePath.Contains(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Partial match found: {FileName} contains {SignaturePath}", fileName, signaturePath);
                            return true;
                        }
                        
                        return false;
                    });

                    if (matchingFile != null)
                    {
                        _logger.LogInformation("Found matching signature file: {MatchingFile}", matchingFile);
                        signatureBytes = File.ReadAllBytes(matchingFile);
                        signatureFileName = SystemPath.GetFileName(matchingFile);
                        fullSignaturePath = matchingFile;
                    }
                    else
                    {
                        _logger.LogWarning("No matching signature file found for: {SignaturePath}", signaturePath);
                        _logger.LogWarning("Available files: {AvailableFiles}", string.Join(", ", signatureFiles.Select(f => SystemPath.GetFileName(f))));
                    }
                }
            }

            if (signatureBytes == null)
            {
                _logger.LogWarning("Could not find any signature file. Tried database lookup, direct path, common locations, and fallback. Signature will not be inserted.");
                return;
            }

            _logger.LogInformation("Using signature file: {FullSignaturePath}", fullSignaturePath);
            _logger.LogInformation("Signature file size: {FileSize} bytes", signatureBytes.Length);

            // Clean up the signature image before inserting
            _logger.LogInformation("Cleaning up signature image: {FileName}", signatureFileName);
            var cleanedSignatureBytes = _signatureCleanupService.CleanupSignature(signatureBytes, signatureFileName);

            // Process signature using OpenXML
            bool signatureFound = await ProcessSignatureWithOpenXML(docxStream, cleanedSignatureBytes, signatureFileName);
            
            if (signatureFound)
            {
                _logger.LogInformation("=== SIGNATURE PROCESSING COMPLETE - SUCCESS ===");
            }
            else
            {
                _logger.LogWarning("=== SIGNATURE PROCESSING COMPLETE - FAILED ===");
                _logger.LogWarning("Could not find or process signature content control in the document");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting signature image: {SignaturePath}", signaturePath);
        }
    }

    private bool IsSignatureControl(string tagName, string title)
    {
        // Check for exact match with "Signature" tag (case-insensitive)
        var tagLower = tagName.ToLower();
        var titleLower = title.ToLower();
        
        var isSignature = tagLower == "signature" || 
                         tagLower.Contains("signature") || tagLower.Contains("sig") ||
                         titleLower.Contains("signature") || titleLower.Contains("sig") ||
                         tagLower.Contains("sign") || titleLower.Contains("sign") ||
                         tagLower.Contains("authorized") || titleLower.Contains("authorized") ||
                         tagLower.Contains("signatory") || titleLower.Contains("signatory");
        
        _logger.LogInformation("Checking if control is signature - Tag: '{TagName}', Title: '{Title}', IsSignature: {IsSignature}", 
            tagName, title, isSignature);
        
        return isSignature;
    }

    private bool InsertSignatureIntoControl(InlineContentControl control, byte[] signatureBytes, WordDocument wordDoc)
    {
        try
        {
            _logger.LogInformation("Attempting to insert signature into control of type: {Type}", control.GetType().Name);
            
            // Clear the existing content
            control.ParagraphItems.Clear();
            
            // Insert the image using byte array
            using var imageStream = new MemoryStream(signatureBytes);
            var picture = new WPicture(wordDoc);
            picture.LoadImage(imageStream);
            
            // Set appropriate dimensions for signature
            picture.Width = 200; // Increased width for better visibility
            picture.Height = 100; // Increased height for better visibility
            
            // Set image properties
            picture.HorizontalAlignment = ShapeHorizontalAlignment.Left;
            picture.VerticalAlignment = ShapeVerticalAlignment.Top;
            
            // Add the picture to the content control
            control.ParagraphItems.Add(picture);
            
            _logger.LogInformation("Successfully inserted signature image into {Type} with dimensions {Width}x{Height}", 
                control.GetType().Name, picture.Width, picture.Height);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting signature into control of type: {Type}", control.GetType().Name);
            return false;
        }
    }

    private async Task<string> GetSignatureFilePath(string fileId)
    {
        try
        {
            _logger.LogInformation("Looking up FileReference for FileId: {FileId}", fileId);
            
            // Parse the fileId as a GUID
            if (!Guid.TryParse(fileId, out var fileGuid))
            {
                _logger.LogWarning("Invalid FileId format: {FileId}", fileId);
                return string.Empty; // Don't use fallback, let the user know the signature is invalid
            }

            // Look up the FileReference from the database
            var fileReference = await _dbContext.FileReferences
                .FirstOrDefaultAsync(fr => fr.Id == fileGuid);

            if (fileReference == null)
            {
                _logger.LogWarning("FileReference not found for FileId: {FileId}", fileId);
                return string.Empty; // Don't use fallback, let the user know the signature is invalid
            }

            _logger.LogInformation("Found FileReference - FileName: {FileName}, FilePath: {FilePath}", 
                fileReference.FileName, fileReference.FilePath);

            // Check if the file actually exists
            if (!File.Exists(fileReference.FilePath))
            {
                _logger.LogWarning("File does not exist at path: {FilePath}", fileReference.FilePath);
                return string.Empty; // Don't use fallback, let the user know the signature file is missing
            }

            // Return the full file path
            return fileReference.FilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up FileReference for FileId: {FileId}", fileId);
            return string.Empty; // Don't use fallback, let the user know there was an error
        }
    }

    private string GetFallbackSignaturePath()
    {
        try
        {
            _logger.LogInformation("Looking for fallback signature file");
            
            // Look for any existing signature file in the uploads/signature directory
            var signatureDir = SystemPath.Combine("uploads", "signature");
            if (Directory.Exists(signatureDir))
            {
                var signatureFiles = Directory.GetFiles(signatureDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => SystemPath.GetExtension(f).ToLower() == ".jpg" || 
                               SystemPath.GetExtension(f).ToLower() == ".jpeg" || 
                               SystemPath.GetExtension(f).ToLower() == ".png")
                    .ToArray();

                if (signatureFiles.Length > 0)
                {
                    var fallbackFile = signatureFiles[0];
                    _logger.LogInformation("Using fallback signature file: {FilePath}", fallbackFile);
                    return fallbackFile;
                }
            }

            _logger.LogWarning("No fallback signature file found");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fallback signature path");
            return string.Empty;
        }
    }

    /// <summary>
    /// Process signature using OpenXML - specifically targets picture content controls
    /// </summary>
    private async Task<bool> ProcessSignatureWithOpenXML(MemoryStream docxStream, byte[] cleanedImageBytes, string signatureFileName)
    {
        try
        {
            _logger.LogInformation("Processing signature with OpenXML approach - targeting picture content controls");
            
            using var wordDoc = WordprocessingDocument.Open(docxStream, true);
            var mainPart = wordDoc.MainDocumentPart;
            
            if (mainPart?.Document == null)
            {
                _logger.LogWarning("Main document part or document is null");
                return false;
            }
            
            // Look for all structured document tags (content controls)
            var sdtElements = mainPart.Document.Descendants<SdtElement>().ToList();
            _logger.LogInformation("Found {Count} SDT elements in document", sdtElements.Count);

            // Log all SDT elements and their tags for debugging
            foreach (var sdt in sdtElements)
            {
                var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                var hasDrawing = sdt.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any();
                var hasBlip = sdt.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();
                _logger.LogInformation("SDT Element - Tag: '{Tag}', ElementType: {ElementType}, HasDrawing: {HasDrawing}, HasBlip: {HasBlip}", 
                    tag ?? "NO_TAG", sdt.GetType().Name, hasDrawing, hasBlip);
            }

            foreach (var sdt in sdtElements)
            {
                var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                
                if (tag == null)
                {
                    _logger.LogWarning("Encountered SDT element without tag. Skipping.");
                    continue;
                }

                _logger.LogInformation("Processing SDT with tag: {Tag}", tag);

                if (tag.Trim() == "Signature")
                {
                    try
                    {
                        _logger.LogInformation("Found Signature tag, checking if it's a picture content control");
                        _logger.LogInformation("SDT element type: {SdtType}", sdt.GetType().Name);
                        _logger.LogInformation("SDT descendants count: {DescendantsCount}", sdt.Descendants().Count());

                        // Check if this is a picture content control by looking for drawing elements or blips
                        bool isPictureContentControl = sdt.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any() ||
                                                      sdt.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();

                        _logger.LogInformation("Is picture content control: {IsPicture}", isPictureContentControl);

                        if (isPictureContentControl)
                        {
                            _logger.LogInformation("Processing as picture content control");
                            
                            // Method 1: Look for existing image blip to replace
                            var blip = sdt.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                            if (blip != null)
                            {
                                _logger.LogInformation("Found existing blip element, attempting to replace image");
                                var imagePartId = blip.Embed?.Value;
                                if (imagePartId != null)
                                {
                                    var imagePart = (ImagePart)mainPart.GetPartById(imagePartId);

                                    // Use the cleaned image bytes
                                    using var imgStream = new MemoryStream(cleanedImageBytes);
                                    imagePart.FeedData(imgStream);
                                    _logger.LogInformation("Successfully replaced Signature image with cleaned version");
                                    return true;
                                }
                            }
                            
                            // Method 2: Look for drawing element with blip
                            var drawing = sdt.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().FirstOrDefault();
                            if (drawing != null)
                            {
                                _logger.LogInformation("Found drawing element, attempting to process");
                                var drawingBlip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                                if (drawingBlip?.Embed?.Value != null)
                                {
                                    _logger.LogInformation("Found blip in drawing element, attempting to replace image");
                                    var imagePartId = drawingBlip.Embed.Value;
                                    var imagePart = (ImagePart)mainPart.GetPartById(imagePartId);

                                    // Use the cleaned image bytes
                                    using var imgStream = new MemoryStream(cleanedImageBytes);
                                    imagePart.FeedData(imgStream);
                                    _logger.LogInformation("Successfully replaced Signature image using drawing element");
                                    return true;
                                }
                            }
                            
                            // Method 3: If no existing image found, try to insert a new one
                            _logger.LogInformation("No existing image found in picture content control, attempting to insert new signature image");
                            var success = await InsertNewSignatureImage(mainPart, sdt, cleanedImageBytes, signatureFileName);
                            if (success)
                            {
                                _logger.LogInformation("Successfully inserted new signature image into picture content control");
                                return true;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Signature tag found but not in a picture content control");
                            _logger.LogWarning("Available descendants in SDT: {Descendants}", string.Join(", ", sdt.Descendants().Select(d => d.GetType().Name)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error replacing signature image in OpenXML");
                    }
                }
            }

            // Also look for text placeholders that might contain signature references
            var allTextElements = mainPart.Document?.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().ToList() ?? new List<DocumentFormat.OpenXml.Wordprocessing.Text>();
            var signatureTextElements = allTextElements.Where(t => t.Text?.Contains("Signature") == true || t.Text?.Contains("{{Signature}}") == true).ToList();
            if (signatureTextElements.Any())
            {
                _logger.LogInformation("Found {Count} text elements containing 'Signature' references", signatureTextElements.Count);
                foreach (var textElement in signatureTextElements)
                {
                    _logger.LogInformation("Text element contains: {Text}", textElement.Text);
                }
            }

            // Check if there are any Signature content controls at all (even if not picture controls)
            var signatureControls = sdtElements.Where(sdt => 
            {
                var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                return tag?.Trim() == "Signature";
            }).ToList();

            if (signatureControls.Any())
            {
                _logger.LogWarning("Found {Count} Signature content controls but none are picture content controls", signatureControls.Count);
                foreach (var control in signatureControls)
                {
                    var hasDrawing = control.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any();
                    var hasBlip = control.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();
                    _logger.LogWarning("Signature control - HasDrawing: {HasDrawing}, HasBlip: {HasBlip}, Type: {Type}", 
                        hasDrawing, hasBlip, control.GetType().Name);
                }
            }
            else
            {
                _logger.LogWarning("No content controls with 'Signature' tag found in the document");
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature with OpenXML");
            return false;
        }
    }

    /// <summary>
    /// Insert a new signature image into an empty content control
    /// </summary>
    private Task<bool> InsertNewSignatureImage(MainDocumentPart mainPart, SdtElement sdt, byte[] imageBytes, string fileName)
    {
        try
        {
            _logger.LogInformation("Attempting to insert new signature image into content control");
            
            // Determine image format based on file extension
            var extension = SystemPath.GetExtension(fileName).ToLower();
            var imageType = extension switch
            {
                ".jpg" or ".jpeg" => ImagePartType.Jpeg,
                ".png" => ImagePartType.Png,
                ".gif" => ImagePartType.Gif,
                ".bmp" => ImagePartType.Bmp,
                _ => ImagePartType.Jpeg
            };

            // Add image part to the document
            var imagePart = mainPart.AddImagePart(imageType);
            using (var imageStream = new MemoryStream(imageBytes))
            {
                imagePart.FeedData(imageStream);
            }

            // Get the relationship ID
            var imagePartId = mainPart.GetIdOfPart(imagePart);
            
            _logger.LogInformation("Added image part with ID: {ImagePartId}", imagePartId);

            // Create a new paragraph with the image
            var paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var run = new DocumentFormat.OpenXml.Wordprocessing.Run();
            
            // Create the drawing element
            var drawing = new DocumentFormat.OpenXml.Wordprocessing.Drawing();
            
            // Create the inline element
            var inline = new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline();
            inline.DistanceFromTop = 0;
            inline.DistanceFromBottom = 0;
            inline.DistanceFromLeft = 0;
            inline.DistanceFromRight = 0;
            
            // Set the size (you can adjust these values as needed)
            var extent = new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent();
            extent.Cx = 2000000; // Width in EMUs (about 2 inches)
            extent.Cy = 500000;  // Height in EMUs (about 0.5 inches)
            inline.Append(extent);
            
            // Create the graphic element
            var graphic = new DocumentFormat.OpenXml.Drawing.Graphic();
            var graphicData = new DocumentFormat.OpenXml.Drawing.GraphicData();
            graphicData.Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture";
            
            // Create the picture element
            var picture = new DocumentFormat.OpenXml.Drawing.Pictures.Picture();
            
            // Create the nonVisualPictureProperties
            var nonVisualPictureProperties = new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties();
            var nonVisualDrawingProperties = new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties();
            nonVisualDrawingProperties.Id = 1;
            nonVisualDrawingProperties.Name = "Signature";
            nonVisualPictureProperties.Append(nonVisualDrawingProperties);
            nonVisualPictureProperties.Append(new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties());
            picture.Append(nonVisualPictureProperties);
            
            // Create the blip fill
            var blipFill = new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill();
            var blip = new Blip();
            blip.Embed = imagePartId;
            blipFill.Append(blip);
            
            // Create the stretch
            var stretch = new Stretch();
            var fillRectangle = new FillRectangle();
            stretch.Append(fillRectangle);
            blipFill.Append(stretch);
            picture.Append(blipFill);
            
            // Create the shape properties
            var shapeProperties = new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties();
            var transform2D = new DocumentFormat.OpenXml.Drawing.Transform2D();
            var offset = new DocumentFormat.OpenXml.Drawing.Offset();
            offset.X = 0;
            offset.Y = 0;
            transform2D.Append(offset);
            var extents = new DocumentFormat.OpenXml.Drawing.Extents();
            extents.Cx = 2000000;
            extents.Cy = 500000;
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);
            
            // Create the preset geometry
            var presetGeometry = new DocumentFormat.OpenXml.Drawing.PresetGeometry();
            presetGeometry.Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle;
            presetGeometry.Append(new DocumentFormat.OpenXml.Drawing.AdjustValueList());
            shapeProperties.Append(presetGeometry);
            
            // Create the solid fill
            var solidFill = new DocumentFormat.OpenXml.Drawing.SolidFill();
            var schemeColor = new DocumentFormat.OpenXml.Drawing.SchemeColor();
            schemeColor.Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.Background1;
            solidFill.Append(schemeColor);
            shapeProperties.Append(solidFill);
            
            // Create the outline
            var outline = new DocumentFormat.OpenXml.Drawing.Outline();
            var noFill = new DocumentFormat.OpenXml.Drawing.NoFill();
            outline.Append(noFill);
            shapeProperties.Append(outline);
            
            picture.Append(shapeProperties);
            graphicData.Append(picture);
            graphic.Append(graphicData);
            inline.Append(graphic);
            drawing.Append(inline);
            
            run.Append(drawing);
            paragraph.Append(run);
            
            // Clear existing content and add the new paragraph
            // For SdtElement, we need to find the content block differently
            var contentBlock = sdt.Descendants<SdtContentBlock>().FirstOrDefault();
            if (contentBlock != null)
            {
                contentBlock.RemoveAllChildren();
                contentBlock.Append(paragraph);
            }
            else
            {
                // If no content block found, try to append to the element directly
                sdt.Append(paragraph);
            }
            
            _logger.LogInformation("Successfully inserted new signature image into content control");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting new signature image");
            return Task.FromResult(false);
        }
    }
}