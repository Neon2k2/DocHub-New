using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;

namespace DocHub.API.Services;

public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _environment;

    public DocumentGenerationService(
        DocHubDbContext context,
        ILogger<DocumentGenerationService> logger,
        IFileStorageService fileStorageService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _environment = environment;
    }

    public async Task<DocumentGenerationResult> GenerateBulkAsync(DocumentGenerationRequest request)
    {
        var result = new DocumentGenerationResult();
        var startTime = DateTime.UtcNow;

        try
        {
            // Get letter type definition
            var letterType = await _context.LetterTypeDefinitions
                .FirstOrDefaultAsync(lt => lt.Id == request.LetterTypeDefinitionId);

            if (letterType == null)
            {
                result.Errors.Add("Letter type definition not found");
                return result;
            }

            // Get employees
            var employees = await _context.Employees
                .Where(e => request.EmployeeIds.Contains(e.Id))
                .ToListAsync();

            if (!employees.Any())
            {
                result.Errors.Add("No employees found");
                return result;
            }

            // Get template
            DocumentTemplate? template = null;
            if (request.TemplateId.HasValue)
            {
                template = await _context.DocumentTemplates
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value);
            }

            // Get signature
            Signature? signature = null;
            if (request.SignatureId.HasValue)
            {
                signature = await _context.Signatures
                    .FirstOrDefaultAsync(s => s.Id == request.SignatureId.Value);
            }

            // Generate documents for each employee
            foreach (var employee in employees)
            {
                try
                {
                    var document = await GenerateDocumentAsync(
                        letterType, 
                        employee, 
                        template, 
                        signature, 
                        request.AdditionalFieldData);

                    if (document != null)
                    {
                        result.GeneratedDocuments.Add(new GeneratedDocumentSummary
                        {
                            Id = document.Id,
                            EmployeeId = document.EmployeeId,
                            EmployeeName = employee.Name,
                            FileName = document.FileName,
                            FileUrl = document.DownloadUrl ?? document.FilePath,
                            GeneratedAt = document.GeneratedAt,
                            GeneratedBy = document.GeneratedBy,
                            Success = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate document for employee {EmployeeId}", employee.Id);
                    result.Errors.Add($"Failed to generate document for {employee.Name}: {ex.Message}");
                }
            }

            result.Success = result.GeneratedDocuments.Any();
            result.Warnings.Add($"Generated {result.GeneratedDocuments.Count} of {employees.Count} documents");

            _logger.LogInformation("Generated {Count} documents for letter type {LetterType} in {Duration}ms",
                result.GeneratedDocuments.Count,
                letterType.TypeKey,
                (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate bulk documents");
            result.Errors.Add($"Bulk generation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<DocumentGenerationResult> GenerateSingleAsync(SingleDocumentGenerationRequest request)
    {
        var result = new DocumentGenerationResult();

        try
        {
            // Get letter type definition
            var letterType = await _context.LetterTypeDefinitions
                .FirstOrDefaultAsync(lt => lt.Id == request.LetterTypeDefinitionId);

            if (letterType == null)
            {
                result.Errors.Add("Letter type definition not found");
                return result;
            }

            // Get employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

            if (employee == null)
            {
                result.Errors.Add("Employee not found");
                return result;
            }

            // Get template
            DocumentTemplate? template = null;
            if (request.TemplateId.HasValue)
            {
                template = await _context.DocumentTemplates
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value);
            }

            // Get signature
            Signature? signature = null;
            if (request.SignatureId.HasValue)
            {
                signature = await _context.Signatures
                    .FirstOrDefaultAsync(s => s.Id == request.SignatureId.Value);
            }

            // Generate document
            var document = await GenerateDocumentAsync(
                letterType, 
                employee, 
                template, 
                signature, 
                request.AdditionalFieldData);

            if (document != null)
            {
                    result.GeneratedDocuments.Add(new GeneratedDocumentSummary
                    {
                        Id = document.Id,
                        EmployeeId = document.EmployeeId,
                        EmployeeName = employee.Name,
                        FileName = document.FileName,
                        FileUrl = document.DownloadUrl ?? document.FilePath,
                        GeneratedAt = document.GeneratedAt,
                        GeneratedBy = document.GeneratedBy,
                        Success = true
                    });
                result.Success = true;
            }
            else
            {
                result.Errors.Add("Failed to generate document");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate single document");
            result.Errors.Add($"Document generation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<DocumentPreviewResult> PreviewAsync(DocumentPreviewRequest request)
    {
        var result = new DocumentPreviewResult();

        try
        {
            // Get letter type definition
            var letterType = await _context.LetterTypeDefinitions
                .FirstOrDefaultAsync(lt => lt.Id == request.LetterTypeDefinitionId);

            if (letterType == null)
            {
                result.Error = "Letter type definition not found";
                return result;
            }

            // Get employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

            if (employee == null)
            {
                result.Error = "Employee not found";
                return result;
            }

            // Get template
            DocumentTemplate? template = null;
            if (request.TemplateId.HasValue)
            {
                template = await _context.DocumentTemplates
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value);
            }

            // Get signature
            Signature? signature = null;
            if (request.SignatureId.HasValue)
            {
                signature = await _context.Signatures
                    .FirstOrDefaultAsync(s => s.Id == request.SignatureId.Value);
            }

            // Generate preview document
            var document = await GenerateDocumentAsync(
                letterType, 
                employee, 
                template, 
                signature, 
                request.AdditionalFieldData,
                isPreview: true);

            if (document != null)
            {
                result.Success = true;
                result.DocumentId = document.Id;
                result.PreviewUrl = document.DownloadUrl ?? "";
            }
            else
            {
                result.Error = "Failed to generate preview";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate document preview");
            result.Error = $"Preview generation failed: {ex.Message}";
        }

        return result;
    }

    public async Task<ValidationResult> ValidateAsync(DocumentValidationRequest request)
    {
        var result = new ValidationResult();

        try
        {
            // Get letter type definition
            var letterType = await _context.LetterTypeDefinitions
                .FirstOrDefaultAsync(lt => lt.Id == request.LetterTypeDefinitionId);

            if (letterType == null)
            {
                result.Errors.Add("Letter type definition not found");
                return result;
            }

            // Get employees
            var employees = await _context.Employees
                .Where(e => request.EmployeeIds.Contains(e.Id))
                .ToListAsync();

            if (!employees.Any())
            {
                result.Errors.Add("No employees found");
                return result;
            }

            // Basic validation - check if employees have required data
            foreach (var employee in employees)
            {
                if (string.IsNullOrWhiteSpace(employee.Name))
                {
                    result.Errors.Add($"Missing employee name for employee ID {employee.Id}");
                }
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate document generation request");
            result.Errors.Add($"Validation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<string> GetDownloadUrlAsync(Guid documentId, string format = "pdf")
    {
        var document = await _context.GeneratedDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new ArgumentException("Document not found");
        }

        // Generate download URL (implement your URL generation logic)
        return $"/api/v1/files/download/{documentId}?format={format}";
    }

    public async Task<byte[]> DownloadDocumentAsync(Guid documentId, string format = "pdf")
    {
        var document = await _context.GeneratedDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new ArgumentException("Document not found");
        }

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Document file not found");
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    private async Task<GeneratedDocument?> GenerateDocumentAsync(
        LetterTypeDefinition letterType,
        Employee employee,
        DocumentTemplate? template,
        Signature? signature,
        Dictionary<string, object>? additionalFieldData,
        bool isPreview = false)
    {
        try
        {
            // Get template path
            string templatePath;
            if (template != null)
            {
                templatePath = Path.Combine(_environment.WebRootPath, template.FilePath.TrimStart('/'));
            }
            else
            {
                // Use default template
                templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "Default.docx");
            }

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", templatePath);
                return null;
            }

            // Generate file name
            var fileName = $"{employee.EmployeeId}_{letterType.TypeKey}_{DateTime.UtcNow:yyyyMMddHHmmss}.docx";
            
            // Create output directory
            var year = DateTime.UtcNow.Year.ToString();
            var month = DateTime.UtcNow.ToString("MMMM");
            var outputDir = Path.Combine("wwwroot", "GeneratedLetters", year, month, letterType.TypeKey);
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, fileName);
            var relativePath = $"/GeneratedLetters/{year}/{month}/{letterType.TypeKey}/{fileName}";

            // Copy template to output location
            File.Copy(templatePath, outputPath, true);

            // Process document with placeholders
            await ProcessDocumentPlaceholdersAsync(outputPath, letterType, employee, additionalFieldData);

            // Create database record
            var document = new GeneratedDocument
            {
                LetterTypeDefinitionId = letterType.Id,
                EmployeeId = employee.Id,
                TemplateId = template?.Id,
                SignatureId = signature?.Id,
                FileName = fileName,
                FilePath = relativePath,
                FileSize = new FileInfo(outputPath).Length,
                GeneratedBy = "System", // TODO: Get from current user context
                GeneratedAt = DateTime.UtcNow,
                DownloadUrl = $"/api/v1/files/download/{Guid.NewGuid()}"
            };

            _context.GeneratedDocuments.Add(document);
            await _context.SaveChangesAsync();

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate document for employee {EmployeeId}", employee.Id);
            return null;
        }
    }

    private async Task ProcessDocumentPlaceholdersAsync(
        string filePath,
        LetterTypeDefinition letterType,
        Employee employee,
        Dictionary<string, object>? additionalFieldData)
    {
        using var doc = WordprocessingDocument.Open(filePath, true);
        var mainPart = doc.MainDocumentPart;
        if (mainPart?.Document?.Body == null) return;

        // Create placeholder replacements
        var replacements = new Dictionary<string, string>
        {
            ["{{EMPLOYEE_NAME}}"] = employee.Name,
            ["{{EMPLOYEE_ID}}"] = employee.EmployeeId,
            ["{{DESIGNATION}}"] = employee.Position,
            ["{{DEPARTMENT}}"] = employee.Department,
            ["{{JOIN_DATE}}"] = employee.JoiningDate.ToString("dd-MMM-yyyy"),
            ["{{RELIEVING_DATE}}"] = employee.RelievingDate?.ToString("dd-MMM-yyyy") ?? "Current",
            ["{{COMPANY_NAME}}"] = "DocHub Technologies",
            ["{{CURRENT_DATE}}"] = DateTime.UtcNow.ToString("dd-MMM-yyyy"),
            ["{{CURRENT_TIME}}"] = DateTime.UtcNow.ToString("HH:mm:ss")
        };

        // Add additional field data
        if (additionalFieldData != null)
        {
            foreach (var kvp in additionalFieldData)
            {
                replacements[$"{{{{{{{kvp.Key}}}}}}}"] = kvp.Value?.ToString() ?? "";
            }
        }

        // Get field data from TabEmployeeData
        var employeeData = await _context.TabEmployeeData
            .FirstOrDefaultAsync(ed => ed.EmployeeId == employee.Id.ToString() && ed.TabId == letterType.Id);

        if (employeeData != null)
        {
            // Add standard fields
            replacements["{{EmployeeId}}"] = employeeData.EmployeeId ?? "";
            replacements["{{EmployeeName}}"] = employeeData.EmployeeName ?? "";
            replacements["{{Email}}"] = employeeData.Email ?? "";
            replacements["{{Phone}}"] = employeeData.Phone ?? "";
            replacements["{{Department}}"] = employeeData.Department ?? "";
            replacements["{{Position}}"] = employeeData.Position ?? "";

            // Add custom fields from JSON
            if (!string.IsNullOrEmpty(employeeData.CustomFields))
            {
                try
                {
                    var customFields = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(employeeData.CustomFields);
                    if (customFields != null)
                    {
                        foreach (var field in customFields)
                        {
                            replacements[$"{{{{{field.Key}}}}}"] = field.Value?.ToString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse custom fields for employee {EmployeeId}", employee.Id);
                }
            }
        }

        // Process all text elements
        foreach (var text in mainPart.Document.Body.Descendants<Text>())
        {
            foreach (var replacement in replacements)
            {
                if (text.Text.Contains(replacement.Key))
                {
                    text.Text = text.Text.Replace(replacement.Key, replacement.Value);
                }
            }
        }

        mainPart.Document.Save();
    }
}
