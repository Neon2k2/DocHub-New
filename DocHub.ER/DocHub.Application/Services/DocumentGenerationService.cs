using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIO;
using System.Text.RegularExpressions;


namespace DocHub.Application.Services;

public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IRepository<GeneratedDocument> _documentRepository;
    private readonly IRepository<DocumentTemplate> _templateRepository;
    private readonly IRepository<Signature> _signatureRepository;
    private readonly IRepository<LetterTypeDefinition> _letterTypeRepository;
    private readonly IRepository<TableSchema> _tableSchemaRepository;
    private readonly IFileManagementService _fileManagementService;
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDbContext _dbContext;

    public DocumentGenerationService(
        IRepository<GeneratedDocument> documentRepository,
        IRepository<DocumentTemplate> templateRepository,
        IRepository<Signature> signatureRepository,
        IRepository<LetterTypeDefinition> letterTypeRepository,
        IRepository<TableSchema> tableSchemaRepository,
        IFileManagementService fileManagementService,
        ILogger<DocumentGenerationService> logger,
        ILoggerFactory loggerFactory,
        IDbContext dbContext)
    {
        _documentRepository = documentRepository;
        _templateRepository = templateRepository;
        _signatureRepository = signatureRepository;
        _letterTypeRepository = letterTypeRepository;
        _tableSchemaRepository = tableSchemaRepository;
        _fileManagementService = fileManagementService;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dbContext = dbContext;
    }

    public async Task<GeneratedDocumentDto> GenerateDocumentAsync(GenerateDocumentRequest request, string userId)
    {
        try
        {
            // Get template and data
        var template = await _templateRepository.GetFirstIncludingAsync(
            t => t.Id == request.TemplateId,
            t => t.File
        );

            if (template == null)
            {
                throw new ArgumentException("Template not found");
            }

            var excelUpload = await _dbContext.ExcelUploads
                .Include(e => e.LetterTypeDefinition)
                .FirstOrDefaultAsync(e => e.Id == request.ExcelUploadId);

            if (excelUpload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            // Get signature if provided
            Signature? signature = null;
            if (request.SignatureId.HasValue)
            {
                signature = await _signatureRepository.GetFirstIncludingAsync(
                    s => s.Id == request.SignatureId.Value,
                    s => s.File
                );
            }

            // Get data from dynamic table
                var dynamicTableService = new DynamicTableService(_dbContext, _tableSchemaRepository, _loggerFactory.CreateLogger<DynamicTableService>());
            var tableData = await dynamicTableService.GetDataFromDynamicTableAsync(
                GetTableNameFromExcelUpload(excelUpload), 0, 1);
            
            if (!tableData.Any())
            {
                throw new ArgumentException("No data found in dynamic table");
            }

            var tabData = tableData.First();

            // Generate document
            var generatedDocument = await ProcessDocumentWithOpenXmlAsync(template, tabData, signature, request.ProcessingOptions);

            // Save generated document
            var documentRecord = new GeneratedDocument
            {
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                ExcelUploadId = request.ExcelUploadId,
                TemplateId = request.TemplateId,
                SignatureId = request.SignatureId,
                FileId = generatedDocument.FileId,
                GeneratedBy = Guid.Parse(userId),
                GeneratedAt = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(new { ProcessingOptions = request.ProcessingOptions }),
            };

            await _documentRepository.AddAsync(documentRecord);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Generated document {DocumentId} by user {UserId}", documentRecord.Id, userId);

            return MapToGeneratedDocumentDto(documentRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document");
            throw;
        }
    }

    public async Task<IEnumerable<GeneratedDocumentDto>> GenerateBulkDocumentsAsync(GenerateBulkDocumentsRequest request, string userId)
    {
        try
        {
            var results = new List<GeneratedDocumentDto>();

            foreach (var excelUploadId in request.ExcelUploadIds)
            {
                var generateRequest = new GenerateDocumentRequest
                {
                    LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                    ExcelUploadId = excelUploadId,
                    TemplateId = request.TemplateId,
                    SignatureId = request.SignatureId,
                    ProcessingOptions = request.ProcessingOptions
                };

                var document = await GenerateDocumentAsync(generateRequest, userId);
                results.Add(document);
            }

            _logger.LogInformation("Generated {Count} bulk documents by user {UserId}", results.Count, userId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulk documents");
            throw;
        }
    }

    public async Task<DocumentPreviewDto> PreviewDocumentAsync(PreviewDocumentRequest request, string userId)
    {
        try
        {
            // Get template and data
        var template = await _templateRepository.GetFirstIncludingAsync(
            t => t.Id == request.TemplateId,
            t => t.File
        );

            if (template == null)
            {
                throw new ArgumentException("Template not found");
            }

            var excelUpload = await _dbContext.ExcelUploads
                .Include(e => e.LetterTypeDefinition)
                .FirstOrDefaultAsync(e => e.Id == request.ExcelUploadId);

            if (excelUpload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            // Get data from dynamic table
                var dynamicTableService = new DynamicTableService(_dbContext, _tableSchemaRepository, _loggerFactory.CreateLogger<DynamicTableService>());
            var tableData = await dynamicTableService.GetDataFromDynamicTableAsync(
                GetTableNameFromExcelUpload(excelUpload), 0, 1);
            
            if (!tableData.Any())
            {
                throw new ArgumentException("No data found in dynamic table");
            }

            var tabData = tableData.First();

            // Generate preview document
            var previewDocument = await ProcessDocumentWithOpenXmlAsync(template, tabData, null, request.ProcessingOptions);

            return new DocumentPreviewDto
            {
                Id = Guid.NewGuid(),
                FileName = $"preview_{template.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.docx",
                MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = previewDocument.FileSize,
                PreviewUrl = $"/api/files/{previewDocument.FileId}/download",
                GeneratedAt = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(new { IsPreview = true, ProcessingOptions = request.ProcessingOptions })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing document");
            throw;
        }
    }

    public async Task<Stream> DownloadDocumentAsync(Guid documentId, string format = "docx")
    {
        try
        {
            var document = await _documentRepository.GetFirstIncludingAsync(
                d => d.Id == documentId,
                d => d.File
            );

            if (document == null)
            {
                throw new ArgumentException("Document not found");
            }

            return await _fileManagementService.DownloadFileAsync(document.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<DocumentTemplateDto> ProcessTemplateAsync(ProcessTemplateRequest request, string userId)
    {
        try
        {
        var template = await _templateRepository.GetFirstIncludingAsync(
            t => t.Id == request.TemplateId,
            t => t.File
        );

            if (template == null)
            {
                throw new ArgumentException("Template not found");
            }

            // Extract placeholders from template
            var placeholders = await ExtractPlaceholdersFromTemplateAsync(template);

            // Update template with extracted placeholders
            template.Placeholders = JsonSerializer.Serialize(placeholders);
            // Note: template properties are typically immutable after creation

            await _templateRepository.UpdateAsync(template);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Processed template {TemplateId} by user {UserId}", request.TemplateId, userId);

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
                UpdatedAt = template.CreatedAt  // Using CreatedAt as UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template {TemplateId}", request.TemplateId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ExtractPlaceholdersAsync(Guid templateId)
    {
        try
        {
            var template = await _templateRepository.GetFirstIncludingAsync(
                t => t.Id == templateId,
                t => t.File
            );

            if (template == null)
            {
                throw new ArgumentException("Template not found");
            }

            return await ExtractPlaceholdersFromTemplateAsync(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting placeholders from template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<bool> ValidateTemplateAsync(Guid templateId)
    {
        try
        {
            var template = await _templateRepository.GetFirstIncludingAsync(
                t => t.Id == templateId,
                t => t.File
            );

            if (template == null)
            {
                return false;
            }

            // Check if file exists and is accessible
            if (template.File == null || !File.Exists(template.File.FilePath))
            {
                return false;
            }

            // Try to open the document to validate it's a valid Word document
            try
            {
                using var stream = new FileStream(template.File.FilePath, FileMode.Open, FileAccess.Read);
                using var document = new WordDocument(stream, FormatType.Docx);
                return document != null;
            }
            catch
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template {TemplateId}", templateId);
            return false;
        }
    }

    public async Task<GeneratedDocumentDto> ProcessDocumentAsync(ProcessDocumentRequest request, string userId)
    {
        try
        {
            // Get the document to process
            var document = await _documentRepository.GetFirstIncludingAsync(
                d => d.Id == request.DocumentId,
                d => d.File,
                d => d.LetterTypeDefinition
            );

            if (document == null)
            {
                throw new ArgumentException("Document not found");
            }

            // Process the document based on options
            var processingOptions = JsonSerializer.Deserialize<Dictionary<string, object>>(request.ProcessingOptions ?? "{}");

            // For now, return the existing document
            // In a real implementation, you would apply specific processing based on options
            _logger.LogInformation("Processed document {DocumentId} by user {UserId}", request.DocumentId, userId);

            return MapToGeneratedDocumentDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", request.DocumentId);
            throw;
        }
    }

    public Task<byte[]> ConvertDocumentAsync(byte[] documentData, string fromFormat, string toFormat)
    {
        try
        {
            // For now, return the original data
            // In a real implementation, you would use libraries like Aspose.Words, DocX, or other conversion tools
            _logger.LogInformation("Document conversion from {FromFormat} to {ToFormat} requested", fromFormat, toFormat);
            return Task.FromResult(documentData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting document from {FromFormat} to {ToFormat}", fromFormat, toFormat);
            throw;
        }
    }

    public Task<DocumentPreviewDto> GeneratePreviewAsync(byte[] documentData, string format)
    {
        try
        {
            // For now, return a basic preview DTO
            // In a real implementation, you would generate an actual preview
            return Task.FromResult(new DocumentPreviewDto
            {
                Id = Guid.NewGuid(),
                FileName = $"preview_{DateTime.UtcNow:yyyyMMddHHmmss}.{format}",
                MimeType = format.ToLowerInvariant() switch
                {
                    "pdf" => "application/pdf",
                    "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                },
                FileSize = documentData.Length,
                PreviewUrl = "/api/files/preview",
                GeneratedAt = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(new { Format = format, IsPreview = true })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for format {Format}", format);
            throw;
        }
    }

    public async Task<GeneratedDocumentDto> InsertSignatureIntoDocumentAsync(InsertSignatureRequest request, string userId)
    {
        try
        {
            var document = await _documentRepository.GetFirstIncludingAsync(
                d => d.Id == request.DocumentId,
                d => d.File!,
                d => d.Signature!
            );

            if (document == null)
            {
                throw new ArgumentException("Document not found");
            }

            var signature = await _signatureRepository.GetFirstIncludingAsync(
                s => s.Id == request.SignatureId,
                s => s.File
            );

            if (signature == null)
            {
                throw new ArgumentException("Signature not found");
            }

            // Process signature insertion
            var processedDocument = await ProcessSignatureInsertionAsync(document, signature, request.Position, request.ProcessingOptions);

            // Update document record
            document.SignatureId = request.SignatureId;
            // Note: GeneratedDocument uses GeneratedAt property

            await _documentRepository.UpdateAsync(document);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Inserted signature into document {DocumentId} by user {UserId}", request.DocumentId, userId);

            return MapToGeneratedDocumentDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting signature into document {DocumentId}", request.DocumentId);
            throw;
        }
    }

    public Task<byte[]> ProcessSignatureAsync(byte[] signatureData, WatermarkRemovalOptions options)
    {
        try
        {
            // For now, return the original signature data
            // In a real implementation, you would use image processing libraries like OpenCV, ImageSharp, or similar
            _logger.LogInformation("Signature processing requested with method: {Method}", options.Method);
            return Task.FromResult(signatureData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature");
            throw;
        }
    }

    public Task<bool> ValidateSignatureQualityAsync(byte[] signatureData)
    {
        try
        {
            // Basic validation - check if data exists and has reasonable size
            if (signatureData == null || signatureData.Length == 0)
                return Task.FromResult(false);

            // Check minimum size (at least 1KB)
            if (signatureData.Length < 1024)
                return Task.FromResult(false);

            // Check maximum size (less than 10MB)
            if (signatureData.Length > 10 * 1024 * 1024)
                return Task.FromResult(false);

            _logger.LogInformation("Signature quality validation completed");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature quality");
            return Task.FromResult(false);
        }
    }

    private async Task<(Guid FileId, long FileSize)> ProcessDocumentWithOpenXmlAsync(
        DocumentTemplate template,
        Dictionary<string, object> tabData,
        Signature? signature,
        string? processingOptions)
    {
        try
        {
            // Read template file
            var templateBytes = await File.ReadAllBytesAsync(template.File!.FilePath);

            // Create a temporary file for processing
            var tempFilePath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFilePath, templateBytes);

            // Process document with Syncfusion
            using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite))
            using (var document = new WordDocument(fileStream, FormatType.Docx))
            {
                // Parse data from JSON
                var data = tabData;

                // Replace placeholders in document
                await ReplacePlaceholdersInDocumentAsync(document, data);

                // Insert signature if provided
                if (signature != null)
                {
                    await InsertSignatureIntoDocumentAsync(document, signature);
                }

                // Save the processed document
                document.Save(fileStream, FormatType.Docx);
            }

            // Read processed document
            var processedBytes = await File.ReadAllBytesAsync(tempFilePath);
            var fileSize = processedBytes.Length;

            // Save processed document
            var fileName = $"generated_{template.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.docx";
            var filePath = Path.Combine("uploads", "documents", fileName);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, processedBytes);

            // Create file reference
            var fileReference = new FileReference
            {
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileSize,
                MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                Category = "document",
                SubCategory = "generated",
                UploadedBy = Guid.Empty, // System generated
                UploadedAt = DateTime.UtcNow
            };

            // Clean up temp file
            File.Delete(tempFilePath);

            return (fileReference.Id, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document with Syncfusion");
            throw;
        }
    }

    private Task ReplacePlaceholdersInDocumentAsync(WordDocument document, Dictionary<string, object> data)
    {
        try
        {
            // Replace placeholders with data using Syncfusion
            foreach (var kvp in data)
            {
                var placeholder = $"{{{kvp.Key}}}";
                var value = kvp.Value?.ToString() ?? string.Empty;
                document.Replace(placeholder, value, false, true);
            }
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing placeholders in document");
            throw;
        }
    }

    private Task InsertSignatureIntoDocumentAsync(WordDocument document, Signature signature)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would:
            // 1. Read the signature image
            // 2. Process it (remove watermarks, resize, etc.)
            // 3. Insert it into the document at the appropriate location
            // 4. Handle positioning and formatting

            // For now, we'll just add a placeholder text
            var lastSection = document.Sections[document.Sections.Count - 1];
            var lastParagraph = lastSection.Paragraphs[lastSection.Paragraphs.Count - 1];
            lastParagraph.AppendText($"\n\nSignature: {signature.Name}");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting signature into document");
            throw;
        }
    }

    private async Task<List<string>> ExtractPlaceholdersFromTemplateAsync(DocumentTemplate template)
    {
        try
        {
            var placeholders = new List<string>();

            if (template.File == null || !File.Exists(template.File.FilePath))
            {
                return placeholders;
            }

            // Read template file
            var templateBytes = await File.ReadAllBytesAsync(template.File.FilePath);

            using (var stream = new MemoryStream(templateBytes))
            using (var document = new WordDocument(stream, FormatType.Docx))
            {
                // Extract placeholders from text using Syncfusion
                var placeholderPattern = @"\{([^}]+)\}";
                var regex = new Regex(placeholderPattern);
                
                // Get all text from the document using Syncfusion's text extraction
                var textSelections = document.FindAll(new Regex(placeholderPattern));
                
                foreach (var selection in textSelections)
                {
                    var text = selection.GetAsOneRange().Text;
                    var matches = regex.Matches(text);
                    foreach (Match match in matches)
                    {
                        var placeholder = match.Groups[1].Value;
                        if (!placeholders.Contains(placeholder))
                        {
                            placeholders.Add(placeholder);
                        }
                    }
                }
            }

            return placeholders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting placeholders from template {TemplateId}", template.Id);
            throw;
        }
    }

    private Task<(Guid FileId, long FileSize)> ProcessSignatureInsertionAsync(
        GeneratedDocument document,
        Signature signature,
        string? position,
        string? processingOptions)
    {
        try
        {
            // This would implement actual signature insertion logic
            // For now, return the existing document
            return Task.FromResult((document.FileId, document.File?.FileSize ?? 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature insertion");
            throw;
        }
    }

    private GeneratedDocumentDto MapToGeneratedDocumentDto(GeneratedDocument document)
    {
        return new GeneratedDocumentDto
        {
            Id = document.Id,
            LetterTypeDefinitionId = document.LetterTypeDefinitionId,
            LetterTypeName = document.LetterTypeDefinition?.DisplayName ?? string.Empty,
            ExcelUploadId = document.ExcelUploadId,
            TemplateId = document.TemplateId,
            TemplateName = document.Template?.Name ?? string.Empty,
            SignatureId = document.SignatureId,
            SignatureName = document.Signature?.Name,
            FileId = document.FileId,
            FileName = document.File?.FileName ?? string.Empty,
            GeneratedBy = document.GeneratedBy,
            GeneratedByName = document.GeneratedByUser?.Username ?? string.Empty,
            GeneratedAt = document.GeneratedAt,
            Metadata = document.Metadata
        };
    }

    private string GetTableNameFromExcelUpload(ExcelUpload excelUpload)
    {
        // Get the table name from the letter type definition's display name
        if (excelUpload.LetterTypeDefinition == null)
        {
            throw new InvalidOperationException("Excel upload has no associated letter type definition");
        }

        // Use the display name from the letter type definition as the base for table name
        var tabName = excelUpload.LetterTypeDefinition.DisplayName;
        if (string.IsNullOrEmpty(tabName))
        {
            throw new InvalidOperationException("Letter type definition has no display name");
        }

        // Clean the tab name to make it a valid table name
        var cleanTabName = System.Text.RegularExpressions.Regex.Replace(tabName, @"[^a-zA-Z0-9_]", "_");
        
        // If we have parsed data with a specific table name, use that
        if (!string.IsNullOrEmpty(excelUpload.ParsedData))
        {
            try
            {
                var parsedData = JsonSerializer.Deserialize<Dictionary<string, object>>(excelUpload.ParsedData);
                if (parsedData != null && parsedData.ContainsKey("TableName"))
                {
                    return parsedData["TableName"].ToString() ?? cleanTabName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing table name from Excel upload data, using tab name: {TabName}", cleanTabName);
            }
        }

        return cleanTabName;
    }
}
