using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Emails;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace DocHub.Application.Services;

public class EmailService : IEmailService
{
    private readonly IRepository<EmailJob> _emailJobRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IRepository<LetterTypeDefinition> _letterTypeRepository;
    private readonly IRepository<GeneratedDocument> _documentRepository;
    private readonly IRepository<TableSchema> _tableSchemaRepository;
    private readonly IFileManagementService _fileManagementService;
    private readonly IRealTimeService _realTimeService;
    private readonly IDbContext _dbContext;
    private readonly ILogger<EmailService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public EmailService(
        IRepository<EmailJob> emailJobRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IRepository<LetterTypeDefinition> letterTypeRepository,
        IRepository<GeneratedDocument> documentRepository,
        IRepository<TableSchema> tableSchemaRepository,
        IFileManagementService fileManagementService,
        IRealTimeService realTimeService,
        IDbContext dbContext,
        ILogger<EmailService> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _emailJobRepository = emailJobRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _letterTypeRepository = letterTypeRepository;
        _documentRepository = documentRepository;
        _tableSchemaRepository = tableSchemaRepository;
        _fileManagementService = fileManagementService;
        _realTimeService = realTimeService;
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
    }

    public async Task<EmailJobDto> SendEmailAsync(SendEmailRequest request, string userId)
    {
        try
        {
            // Validate letter type
            var letterType = await _letterTypeRepository.GetByIdAsync(request.LetterTypeDefinitionId);
            if (letterType == null)
            {
                throw new ArgumentException("Letter type not found");
            }

            var excelUpload = await _dbContext.ExcelUploads
                .Include(e => e.LetterTypeDefinition)
                .FirstOrDefaultAsync(e => e.Id == request.ExcelUploadId);
            if (excelUpload == null)
            {
                throw new ArgumentException("Excel upload not found");
            }

            // Process email template if provided
            string finalSubject = request.Subject;
            string finalContent = request.Content;

            if (request.EmailTemplateId.HasValue)
            {
                var template = await _emailTemplateRepository.GetByIdAsync(request.EmailTemplateId.Value);
                if (template != null)
                {
                    finalSubject = template.Subject;
                    finalContent = template.Content;
                }
            }

            // Create email job
            var emailJob = new EmailJob
            {
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                ExcelUploadId = request.ExcelUploadId,
                DocumentId = request.DocumentId,
                EmailTemplateId = request.EmailTemplateId,
                Subject = finalSubject,
                Content = finalContent,
                Attachments = request.Attachments,
                Status = "pending",
                SentBy = Guid.Parse(userId),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _emailJobRepository.AddAsync(emailJob);
            await _dbContext.SaveChangesAsync();

            // Process email asynchronously
            _ = Task.Run(async () => await ProcessEmailJobAsync(emailJob.Id));

            return await MapToEmailJobDtoAsync(emailJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            throw;
        }
    }

    public async Task<IEnumerable<EmailJobDto>> SendBulkEmailsAsync(SendBulkEmailsRequest request, string userId)
    {
        try
        {
            var results = new List<EmailJobDto>();

            foreach (var excelUploadId in request.ExcelUploadIds)
            {
                var sendRequest = new SendEmailRequest
                {
                    LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                    ExcelUploadId = excelUploadId,
                    DocumentId = null, // Will be determined per record
                    EmailTemplateId = request.EmailTemplateId,
                    Subject = request.Subject,
                    Content = request.Content,
                    Attachments = request.Attachments
                };

                var result = await SendEmailAsync(sendRequest, userId);
                results.Add(result);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk emails");
            throw;
        }
    }

    public async Task<IEnumerable<EmailJobDto>> GetEmailsAsync(string userId, bool isAdmin = false)
    {
        try
        {
            var emailJobs = await _emailJobRepository.GetIncludingAsync(
                e => isAdmin || e.SentBy == Guid.Parse(userId),
                e => e.LetterTypeDefinition!,
                e => e.ExcelUpload!,
                e => e.Document!,
                e => e.EmailTemplate!,
                e => e.SentByUser!
            );

            var result = new List<EmailJobDto>();
            foreach (var job in emailJobs)
            {
                result.Add(await MapToEmailJobDtoAsync(job));
            }
            _logger.LogInformation("Retrieved {Count} email jobs for user {UserId} (admin: {IsAdmin})", 
                result.Count, userId, isAdmin);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emails");
            throw;
        }
    }

    public async Task<EmailJobDto> GetEmailByIdAsync(Guid jobId, string userId, bool isAdmin = false)
    {
        try
        {
            var emailJob = await _emailJobRepository.GetFirstIncludingAsync(
                e => e.Id == jobId && (isAdmin || e.SentBy == Guid.Parse(userId)),
                e => e.LetterTypeDefinition!,
                e => e.ExcelUpload!,
                e => e.Document!,
                e => e.EmailTemplate!,
                e => e.SentByUser!
            );

            if (emailJob == null)
            {
                throw new ArgumentException("Email job not found");
            }

            return await MapToEmailJobDtoAsync(emailJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email by ID");
            throw;
        }
    }

    public async Task<IEnumerable<EmailJobDto>> GetFailedEmailsAsync(string userId, bool isAdmin = false)
    {
        try
        {
            var failedJobs = await _emailJobRepository.GetIncludingAsync(
                e => (isAdmin || e.SentBy == Guid.Parse(userId)) && 
                     (e.Status == "bounced" || e.Status == "dropped" || !string.IsNullOrEmpty(e.ErrorMessage)),
                e => e.LetterTypeDefinition!,
                e => e.ExcelUpload!,
                e => e.Document!,
                e => e.EmailTemplate!,
                e => e.SentByUser!
            );

            var result = new List<EmailJobDto>();
            foreach (var job in failedJobs)
            {
                result.Add(await MapToEmailJobDtoAsync(job));
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed emails");
            throw;
        }
    }

    public async Task ProcessEmailJobAsync(Guid jobId)
    {
        try
        {
            var emailJob = await _emailJobRepository.GetFirstIncludingAsync(
                e => e.Id == jobId,
                e => e.LetterTypeDefinition!,
                e => e.ExcelUpload!,
                e => e.Document!,
                e => e.EmailTemplate!,
                e => e.SentByUser!
            );

            if (emailJob == null)
            {
                _logger.LogWarning("Email job {JobId} not found", jobId);
                return;
            }

            // Extract recipient email and name from dynamic table if not provided
            string toEmail = emailJob.RecipientEmail ?? string.Empty;
            string? toName = emailJob.RecipientName;

            if (string.IsNullOrEmpty(toEmail))
    {
        try
        {
                    // Get data from dynamic table
                    var dynamicTableService = new DynamicTableService(_dbContext, _tableSchemaRepository, _loggerFactory.CreateLogger<DynamicTableService>());
                    var tableName = GetTableNameFromExcelUpload(emailJob.ExcelUpload);
                    var tableData = await dynamicTableService.GetDataFromDynamicTableAsync(tableName, 0, 1);
                    
                    if (tableData.Any())
                    {
                        var dataDict = tableData.First();
                        // Try common email field names
                        var emailFields = new[] { "email", "Email", "email_address", "EmailAddress", "recipient_email" };
                        foreach (var fieldName in emailFields)
                        {
                            if (dataDict.TryGetValue(fieldName, out var emailValue) && !string.IsNullOrEmpty(emailValue?.ToString()))
                            {
                                toEmail = emailValue.ToString()!;
                                break;
                            }
                        }

                        // Try common name field names
                        if (string.IsNullOrEmpty(toName))
                        {
                            var nameFields = new[] { "name", "Name", "full_name", "FullName", "recipient_name", "first_name", "last_name" };
                            var nameValues = new List<string>();
                            
                            foreach (var fieldName in nameFields)
                            {
                                if (dataDict.TryGetValue(fieldName, out var nameValue) && !string.IsNullOrEmpty(nameValue?.ToString()))
                                {
                                    nameValues.Add(nameValue.ToString()!);
                                }
                            }
                            
                            toName = string.Join(" ", nameValues).Trim();
                        }
                    }
        }
        catch (Exception ex)
        {
                    _logger.LogWarning(ex, "Error accessing dynamic table data for email job {EmailJobId}", emailJob.Id);
                }
            }

            if (string.IsNullOrEmpty(toEmail))
            {
                throw new ArgumentException("Recipient email address is required");
            }

            // Send email via SendGrid
            await SendEmailViaSendGridAsync(emailJob, toEmail, toName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email job {JobId}", jobId);
            
            // Update job status to failed
            var emailJob = await _emailJobRepository.GetByIdAsync(jobId);
            if (emailJob != null)
            {
                emailJob.Status = "failed";
                emailJob.ErrorMessage = ex.Message;
            emailJob.UpdatedAt = DateTime.UtcNow;
            await _emailJobRepository.UpdateAsync(emailJob);
            await _dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task SendEmailViaSendGridAsync(EmailJob emailJob, string toEmail, string? toName)
    {
        try
        {
            _logger.LogInformation("Sending email via SendGrid to {Email}", toEmail);

            // Get SendGrid configuration
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("SendGrid API key is not configured");
            }

            // Create SendGrid client
            var client = new SendGrid.SendGridClient(apiKey);

            // Parse attachments if any
            var attachments = new List<SendGrid.Helpers.Mail.Attachment>();
            if (!string.IsNullOrEmpty(emailJob.Attachments))
            {
                try
                {
                    var attachmentsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(emailJob.Attachments);
                    if (attachmentsList != null)
                    {
                        foreach (var attachmentInfo in attachmentsList)
                        {
                            if (attachmentInfo.TryGetValue("fileId", out var fileIdObj) && 
                                Guid.TryParse(fileIdObj.ToString(), out var fileId))
                            {
                                var fileStream = await _fileManagementService.DownloadFileAsync(fileId);
                                var fileBytes = await ReadStreamAsync(fileStream);
                                var fileName = attachmentInfo.GetValueOrDefault("fileName")?.ToString() ?? "attachment";
                                var mimeType = attachmentInfo.GetValueOrDefault("mimeType")?.ToString() ?? "application/octet-stream";
                                
                                var attachment = new SendGrid.Helpers.Mail.Attachment
                                {
                                    Content = Convert.ToBase64String(fileBytes),
                                    Filename = fileName,
                                    Type = mimeType,
                                    Disposition = "attachment"
                                };
                                
                                attachments.Add(attachment);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing email attachments for job {EmailJobId}", emailJob.Id);
                }
            }

            // Create SendGrid message
            var message = new SendGrid.Helpers.Mail.SendGridMessage()
            {
                From = new SendGrid.Helpers.Mail.EmailAddress(fromEmail, fromName),
                Subject = emailJob.Subject,
                PlainTextContent = StripHtml(emailJob.Content),
                HtmlContent = emailJob.Content
            };

            message.AddTo(new SendGrid.Helpers.Mail.EmailAddress(toEmail, toName));

            // Add attachments
            if (attachments.Count > 0)
            {
                message.Attachments = attachments;
            }

            // Send email
            var response = await client.SendEmailAsync(message);

            if (response.IsSuccessStatusCode)
            {
                emailJob.Status = "sent";
                emailJob.SendGridMessageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault();
                emailJob.SentAt = DateTime.UtcNow;
                emailJob.UpdatedAt = DateTime.UtcNow;
                
                await _emailJobRepository.UpdateAsync(emailJob);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Email sent successfully to {Email} with message ID {MessageId}", 
                    toEmail, emailJob.SendGridMessageId);
            }
            else
            {
                throw new HttpRequestException($"SendGrid API returned status code: {response.StatusCode}");
            }

            // Send real-time notification
            await _realTimeService.NotifyEmailStatusUpdateAsync(emailJob.SentBy.ToString(), new EmailStatusUpdate
            {
                EmailJobId = emailJob.Id,
                Status = emailJob.Status,
                Timestamp = DateTime.UtcNow,
                SendGridMessageId = emailJob.SendGridMessageId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SendGrid for job {EmailJobId}", emailJob.Id);
            
            emailJob.Status = "failed";
            emailJob.ErrorMessage = ex.Message;
            emailJob.UpdatedAt = DateTime.UtcNow;
            
            await _emailJobRepository.UpdateAsync(emailJob);
            await _dbContext.SaveChangesAsync();

            // Send failure notification
            await _realTimeService.NotifyEmailStatusUpdateAsync(emailJob.SentBy.ToString(), new EmailStatusUpdate
            {
                EmailJobId = emailJob.Id,
                Status = "failed",
                Timestamp = DateTime.UtcNow,
                Reason = ex.Message
            });
        }
    }

    private async Task<byte[]> ReadStreamAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
    }

    private async Task UpdateEmailStatusFromWebhookAsync(EmailJob emailJob, string? eventType, Dictionary<string, object> payload)
    {
        if (string.IsNullOrEmpty(eventType)) return;

        var timestamp = DateTime.UtcNow;
        if (payload.TryGetValue("timestamp", out var ts) && ts is long unixTime)
        {
            timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
        }

        switch (eventType.ToLowerInvariant())
        {
            case "sent":
                emailJob.Status = "sent";
                emailJob.SentAt = timestamp;
                break;
            case "delivered":
                emailJob.Status = "delivered";
                emailJob.DeliveredAt = timestamp;
                break;
            case "opened":
                emailJob.Status = "opened";
                emailJob.OpenedAt = timestamp;
                break;
            case "clicked":
                emailJob.Status = "clicked";
                emailJob.ClickedAt = timestamp;
                break;
            case "bounced":
                emailJob.Status = "bounced";
                emailJob.BouncedAt = timestamp;
                if (payload.TryGetValue("reason", out var reason))
                {
                    emailJob.ErrorMessage = reason.ToString();
                }
                break;
            case "dropped":
                emailJob.Status = "dropped";
                emailJob.DroppedAt = timestamp;
                if (payload.TryGetValue("reason", out var dropReason))
                {
                    emailJob.ErrorMessage = dropReason.ToString();
                }
                break;
            case "unsubscribe":
                emailJob.Status = "unsubscribed";
                emailJob.UnsubscribedAt = timestamp;
                break;
        }

        emailJob.UpdatedAt = DateTime.UtcNow;
        await _emailJobRepository.UpdateAsync(emailJob);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<EmailJobDto> MapToEmailJobDtoAsync(EmailJob emailJob)
    {
        // Extract employee information from dynamic table
        var employeeId = string.Empty;
        var employeeName = string.Empty;
        var employeeEmail = string.Empty;

        try
        {
            var dynamicTableService = new DynamicTableService(_dbContext, _tableSchemaRepository, _loggerFactory.CreateLogger<DynamicTableService>());
            var tableName = GetTableNameFromExcelUpload(emailJob.ExcelUpload);
            var tableData = await dynamicTableService.GetDataFromDynamicTableAsync(tableName, 0, 1);
            
            if (tableData.Any())
            {
                var dataDict = tableData.First();
                if (dataDict != null)
                {
                    // Try common employee ID field names
                    var idFields = new[] { "employee_id", "employeeId", "EmployeeId", "emp_id", "EmpId", "id", "Id" };
                    foreach (var fieldName in idFields)
                    {
                        if (dataDict.TryGetValue(fieldName, out var idValue) && !string.IsNullOrEmpty(idValue?.ToString()))
                        {
                            employeeId = idValue.ToString()!;
                            break;
                        }
                    }

                    // Try common name field names
                    var nameFields = new[] { "name", "Name", "full_name", "FullName", "recipient_name", "first_name", "last_name" };
                    var firstName = string.Empty;
                    var lastName = string.Empty;
                    
                    foreach (var fieldName in nameFields)
                    {
                        if (dataDict.TryGetValue(fieldName, out var nameValue) && !string.IsNullOrEmpty(nameValue?.ToString()))
                        {
                            employeeName = nameValue.ToString()!;
                            break;
                        }
                    }

                    // Try first_name and last_name separately
                    if (string.IsNullOrEmpty(employeeName))
                    {
                        if (dataDict.TryGetValue("first_name", out var firstNameValue) && !string.IsNullOrEmpty(firstNameValue?.ToString()))
                        {
                            firstName = firstNameValue.ToString()!;
                        }
                        if (dataDict.TryGetValue("last_name", out var lastNameValue) && !string.IsNullOrEmpty(lastNameValue?.ToString()))
                        {
                            lastName = lastNameValue.ToString()!;
                        }
                        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                        {
                            employeeName = $"{firstName} {lastName}".Trim();
                        }
                    }

                    // Try common email field names
                    var emailFields = new[] { "email", "Email", "email_address", "EmailAddress", "recipient_email", "employee_email" };
                    foreach (var fieldName in emailFields)
                    {
                        if (dataDict.TryGetValue(fieldName, out var emailValue) && !string.IsNullOrEmpty(emailValue?.ToString()))
                        {
                            employeeEmail = emailValue.ToString()!;
                            break;
                        }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            _logger.LogWarning(ex, "Error parsing dynamic table data for email job {EmailJobId}", emailJob.Id);
        }

        return new EmailJobDto
        {
            Id = emailJob.Id,
            LetterTypeDefinitionId = emailJob.LetterTypeDefinitionId,
            LetterTypeName = emailJob.LetterTypeDefinition?.DisplayName ?? string.Empty,
            ExcelUploadId = emailJob.ExcelUploadId,
            DocumentId = emailJob.DocumentId,
            DocumentName = emailJob.Document?.Template?.Name ?? string.Empty,
            EmailTemplateId = emailJob.EmailTemplateId,
            EmailTemplateName = emailJob.EmailTemplate?.Name ?? string.Empty,
            Subject = emailJob.Subject,
            Content = emailJob.Content,
            Attachments = emailJob.Attachments,
            Status = emailJob.Status,
            ErrorMessage = emailJob.ErrorMessage,
            SendGridMessageId = emailJob.SendGridMessageId,
            SentBy = emailJob.SentBy,
            SentByName = emailJob.SentByUser?.Username ?? string.Empty,
            SentAt = emailJob.SentAt,
            CreatedAt = emailJob.CreatedAt,
            UpdatedAt = emailJob.UpdatedAt,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            EmployeeEmail = employeeEmail,
            RecipientEmail = emailJob.RecipientEmail,
            RecipientName = emailJob.RecipientName,
            UnsubscribedAt = emailJob.UnsubscribedAt,
            BouncedAt = emailJob.BouncedAt,
            DroppedAt = emailJob.DroppedAt,
            DeliveredAt = emailJob.DeliveredAt,
            OpenedAt = emailJob.OpenedAt,
            ClickedAt = emailJob.ClickedAt,
            ProcessedAt = emailJob.ProcessedAt
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

    // Email Template Management
    public async Task<EmailTemplateDto> CreateEmailTemplateAsync(CreateEmailTemplateRequest request, string userId)
    {
        try
        {
            var template = new EmailTemplate
            {
                Name = request.Name,
                Subject = request.Subject,
                Content = request.Content,
                Placeholders = request.Placeholders,
                IsActive = true,
                CreatedBy = Guid.Parse(userId),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _emailTemplateRepository.AddAsync(template);
            await _dbContext.SaveChangesAsync();

            return MapToEmailTemplateDto(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email template");
            throw;
        }
    }

    public async Task<EmailTemplateDto> UpdateEmailTemplateAsync(Guid templateId, UpdateEmailTemplateRequest request, string userId)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new ArgumentException("Email template not found");
            }

            template.Name = request.Name ?? string.Empty;
            template.Subject = request.Subject ?? string.Empty;
            template.Content = request.Content ?? string.Empty;
            template.Placeholders = request.Placeholders;
            template.IsActive = request.IsActive ?? true;
            template.UpdatedBy = Guid.Parse(userId);
            template.UpdatedAt = DateTime.UtcNow;

            await _emailTemplateRepository.UpdateAsync(template);
            await _dbContext.SaveChangesAsync();

            return MapToEmailTemplateDto(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email template");
            throw;
        }
    }

    public async Task DeleteEmailTemplateAsync(Guid templateId, string userId)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new ArgumentException("Email template not found");
            }

            await _emailTemplateRepository.DeleteAsync(template);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email template");
            throw;
        }
    }

    public async Task<IEnumerable<EmailTemplateDto>> GetEmailTemplatesAsync(string userId)
    {
        try
        {
            var templates = await _emailTemplateRepository.GetAllAsync();
            return templates.Select(MapToEmailTemplateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates");
            throw;
        }
    }

    public async Task<EmailTemplateDto> GetEmailTemplateByIdAsync(Guid templateId, string userId)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new ArgumentException("Email template not found");
            }

            return MapToEmailTemplateDto(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template by ID");
            throw;
        }
    }

    private EmailTemplateDto MapToEmailTemplateDto(EmailTemplate template)
    {
        return new EmailTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            Content = template.Content,
            Placeholders = template.Placeholders,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    // Additional interface methods
    public async Task<EmailJobDto> GetEmailStatusAsync(Guid emailJobId)
    {
        try
        {
            var emailJob = await _emailJobRepository.GetByIdAsync(emailJobId);
            if (emailJob == null)
            {
                throw new ArgumentException("Email job not found");
            }

            return await MapToEmailJobDtoAsync(emailJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email status");
            throw;
        }
    }

    public async Task<IEnumerable<EmailJobDto>> GetEmailJobsAsync(GetEmailJobsRequest request, string userId)
    {
        try
        {
            var query = _dbContext.EmailJobs.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(e => e.Status == request.Status);
            }
            
            if (request.FromDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= request.FromDate.Value);
            }
            
            if (request.ToDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt <= request.ToDate.Value);
            }

            var totalCount = await query.CountAsync();
            var emailJobs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(e => e.LetterTypeDefinition)
                .Include(e => e.ExcelUpload)
                .Include(e => e.Document)
                .Include(e => e.EmailTemplate)
                .Include(e => e.SentByUser)
                .ToListAsync();

            var result = new List<EmailJobDto>();
            foreach (var job in emailJobs)
            {
                result.Add(await MapToEmailJobDtoAsync(job));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email jobs");
            throw;
        }
    }

    public async Task<IEnumerable<EmailTemplateDto>> GetEmailTemplatesAsync(Guid? letterTypeId = null)
    {
        try
        {
            var query = _dbContext.EmailTemplates.AsQueryable();
            
            if (letterTypeId.HasValue)
            {
                // Filter by letter type if needed
                query = query.Where(t => t.IsActive);
            }

            var templates = await query.ToListAsync();
            return templates.Select(MapToEmailTemplateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates");
            throw;
        }
    }

    public async Task<EmailTemplateDto> GetEmailTemplateAsync(Guid templateId)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new ArgumentException("Email template not found");
            }

            return MapToEmailTemplateDto(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template");
            throw;
        }
    }

    public async Task<EmailJobDto> ProcessEmailTemplateAsync(Guid templateId, object data)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new ArgumentException("Email template not found");
            }

            var renderedContent = await RenderEmailContentAsync(template.Content, data);
            
            // Create a mock email job for the processed template
            var mockEmailJob = new EmailJob
            {
                Id = Guid.NewGuid(),
                Subject = template.Subject,
                Content = renderedContent,
                Status = "processed",
                CreatedAt = DateTime.UtcNow
            };
            
            return await MapToEmailJobDtoAsync(mockEmailJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email template");
            throw;
        }
    }

    public async Task<string> RenderEmailContentAsync(string content, object data)
    {
        try
        {
            // Simple template rendering - replace placeholders with data
            var result = content;
            
            if (data != null)
            {
                var properties = data.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var placeholder = $"{{{prop.Name}}}";
                    var value = prop.GetValue(data)?.ToString() ?? string.Empty;
                    result = result.Replace(placeholder, value);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering email content");
            throw;
        }
    }

    public async Task<bool> ValidateEmailAddressAsync(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email address");
            return false;
        }
    }

    public async Task ProcessSendGridWebhookAsync(WebhookEvent webhookEvent)
    {
        try
        {
            // Parse the payload to extract email job information
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(webhookEvent.Payload);
            if (payload == null || !payload.TryGetValue("email_job_id", out var emailJobIdObj))
                return;

            var emailJobId = Guid.Parse(emailJobIdObj.ToString()!);
            var emailJob = await _emailJobRepository.GetByIdAsync(emailJobId);
            
            if (emailJob == null)
            {
                _logger.LogWarning("Email job {EmailJobId} not found for webhook event", emailJobId);
                return;
            }

            await UpdateEmailStatusFromWebhookAsync(emailJob, webhookEvent.EventType, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendGrid webhook");
            throw;
        }
    }

    public async Task UpdateEmailStatusAsync(EmailStatusUpdate statusUpdate)
    {
        try
        {
            var emailJob = await _emailJobRepository.GetByIdAsync(statusUpdate.EmailJobId);
            if (emailJob == null)
            {
                throw new ArgumentException("Email job not found");
            }

            emailJob.Status = statusUpdate.Status;
            emailJob.UpdatedAt = DateTime.UtcNow;

            switch (statusUpdate.Status.ToLowerInvariant())
            {
                case "sent":
                    emailJob.SentAt = statusUpdate.Timestamp;
                    break;
                case "delivered":
                    emailJob.DeliveredAt = statusUpdate.Timestamp;
                    break;
                case "opened":
                    emailJob.OpenedAt = statusUpdate.Timestamp;
                    break;
                case "clicked":
                    emailJob.ClickedAt = statusUpdate.Timestamp;
                    break;
                case "bounced":
                    emailJob.BouncedAt = statusUpdate.Timestamp;
                    break;
                case "dropped":
                    emailJob.DroppedAt = statusUpdate.Timestamp;
                    break;
            }

            if (!string.IsNullOrEmpty(statusUpdate.Reason))
            {
                emailJob.ErrorMessage = statusUpdate.Reason;
            }

            await _emailJobRepository.UpdateAsync(emailJob);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email status");
            throw;
        }
    }

    public async Task ProcessEmailEventAsync(string eventType, object eventData)
    {
        try
        {
            _logger.LogInformation("Processing email event: {EventType}", eventType);
            
            // Convert event data to dictionary for processing
            var dataDict = new Dictionary<string, object>();
            if (eventData != null)
            {
                var properties = eventData.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    dataDict[prop.Name] = prop.GetValue(eventData) ?? string.Empty;
                }
            }

            // Process based on event type
            switch (eventType.ToLowerInvariant())
            {
                case "sent":
                case "delivered":
                case "opened":
                case "clicked":
                case "bounced":
                case "dropped":
                case "unsubscribe":
                    if (dataDict.TryGetValue("email_job_id", out var jobIdObj) && 
                        Guid.TryParse(jobIdObj.ToString(), out var jobId))
                    {
                        var emailJob = await _emailJobRepository.GetByIdAsync(jobId);
                        if (emailJob != null)
                        {
                            await UpdateEmailStatusFromWebhookAsync(emailJob, eventType, dataDict);
                        }
                    }
                    break;
                default:
                    _logger.LogWarning("Unknown email event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email event");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetEmailAnalyticsAsync(Guid? letterTypeId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _dbContext.EmailJobs.AsQueryable();
            
            if (letterTypeId.HasValue)
            {
                query = query.Where(e => e.LetterTypeDefinitionId == letterTypeId.Value);
            }
            
            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt <= endDate.Value);
            }

            var totalEmails = await query.CountAsync();
            var sentEmails = await query.CountAsync(e => e.Status == "sent");
            var deliveredEmails = await query.CountAsync(e => e.Status == "delivered");
            var openedEmails = await query.CountAsync(e => e.Status == "opened");
            var clickedEmails = await query.CountAsync(e => e.Status == "clicked");
            var bouncedEmails = await query.CountAsync(e => e.Status == "bounced");
            var failedEmails = await query.CountAsync(e => e.Status == "failed" || e.Status == "dropped");

            return new Dictionary<string, object>
            {
                ["TotalEmails"] = totalEmails,
                ["SentEmails"] = sentEmails,
                ["DeliveredEmails"] = deliveredEmails,
                ["OpenedEmails"] = openedEmails,
                ["ClickedEmails"] = clickedEmails,
                ["BouncedEmails"] = bouncedEmails,
                ["FailedEmails"] = failedEmails,
                ["DeliveryRate"] = totalEmails > 0 ? (double)deliveredEmails / totalEmails * 100 : 0,
                ["OpenRate"] = deliveredEmails > 0 ? (double)openedEmails / deliveredEmails * 100 : 0,
                ["ClickRate"] = deliveredEmails > 0 ? (double)clickedEmails / deliveredEmails * 100 : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email analytics");
            throw;
        }
    }

    public async Task<IEnumerable<EmailJobDto>> GetFailedEmailsAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var failedJobs = await _dbContext.EmailJobs
                .Where(e => e.Status == "failed" || e.Status == "bounced" || e.Status == "dropped")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(e => e.LetterTypeDefinition)
                .Include(e => e.ExcelUpload)
                .Include(e => e.Document)
                .Include(e => e.EmailTemplate)
                .Include(e => e.SentByUser)
                .ToListAsync();

            var result = new List<EmailJobDto>();
            foreach (var job in failedJobs)
            {
                result.Add(await MapToEmailJobDtoAsync(job));
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed emails");
            throw;
        }
    }

    public async Task<bool> RetryFailedEmailAsync(Guid emailJobId, string userId)
    {
        try
        {
            var emailJob = await _emailJobRepository.GetByIdAsync(emailJobId);
            if (emailJob == null)
            {
                throw new ArgumentException("Email job not found");
            }

            if (emailJob.Status != "failed" && emailJob.Status != "bounced" && emailJob.Status != "dropped")
            {
                throw new InvalidOperationException("Email job is not in a failed state");
            }

            // Reset status and retry
            emailJob.Status = "pending";
            emailJob.ErrorMessage = null;
            emailJob.UpdatedAt = DateTime.UtcNow;

            await _emailJobRepository.UpdateAsync(emailJob);
            await _dbContext.SaveChangesAsync();

            // Process email asynchronously
            _ = Task.Run(async () => await ProcessEmailJobAsync(emailJobId));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed email");
            throw;
        }
    }
}
