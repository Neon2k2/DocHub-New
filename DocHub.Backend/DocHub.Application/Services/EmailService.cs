using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Shared.DTOs.Emails;
using DocHub.Shared.DTOs.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace DocHub.Application.Services;

public class EmailService : IEmailService
{
    private readonly IRepository<EmailJob> _emailJobRepository;
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


            // Create email job
            var emailJob = new EmailJob
            {
                LetterTypeDefinitionId = request.LetterTypeDefinitionId,
                ExcelUploadId = request.ExcelUploadId,
                DocumentId = request.DocumentId,
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
                    // Get data from dynamic table (only if ExcelUpload exists)
                    if (emailJob.ExcelUpload != null)
                    {
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
            var employeeInfo = await GetEmployeeInfoAsync(emailJob);
            await _realTimeService.NotifyEmailStatusUpdateAsync(emailJob.SentBy.ToString(), new EmailStatusUpdate
            {
                EmailJobId = emailJob.Id,
                Status = emailJob.Status,
                Timestamp = DateTime.UtcNow,
                SendGridMessageId = emailJob.SendGridMessageId,
                EmployeeName = employeeInfo.Name,
                EmployeeEmail = employeeInfo.Email
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
            var employeeInfo = await GetEmployeeInfoAsync(emailJob);
            await _realTimeService.NotifyEmailStatusUpdateAsync(emailJob.SentBy.ToString(), new EmailStatusUpdate
            {
                EmailJobId = emailJob.Id,
                Status = "failed",
                Timestamp = DateTime.UtcNow,
                Reason = ex.Message,
                EmployeeName = employeeInfo.Name,
                EmployeeEmail = employeeInfo.Email
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

    private async Task<IEnumerable<EmailJobDto>> MapEmailJobsToDtoBatchAsync(IEnumerable<EmailJob> emailJobs)
    {
        try
        {
            _logger.LogInformation("🔄 [EMAIL-MAPPING] Starting batch mapping for {Count} email jobs", emailJobs.Count());
            
            var result = new List<EmailJobDto>();
            var dynamicTableService = new DynamicTableService(_dbContext, _tableSchemaRepository, _loggerFactory.CreateLogger<DynamicTableService>());
            
            // Group email jobs by ExcelUploadId to batch dynamic table queries
            var jobsByExcelUpload = emailJobs
                .Where(job => job.ExcelUploadId.HasValue)
                .GroupBy(job => job.ExcelUploadId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("📊 [EMAIL-MAPPING] Grouped into {GroupCount} Excel upload groups", jobsByExcelUpload.Count);

            // Cache for dynamic table data to avoid repeated queries
            var dynamicTableDataCache = new Dictionary<Guid, List<Dictionary<string, object>>>();

            foreach (var emailJob in emailJobs)
            {
                try
                {
                    var dto = new EmailJobDto
                    {
                        Id = emailJob.Id,
                        LetterTypeDefinitionId = emailJob.LetterTypeDefinitionId,
                        LetterTypeName = emailJob.LetterTypeDefinition?.DisplayName ?? string.Empty,
                        ExcelUploadId = emailJob.ExcelUploadId,
                        DocumentId = emailJob.DocumentId,
                        DocumentName = emailJob.Document?.Template?.Name ?? string.Empty,
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

                    // Try to get employee info from dynamic table data if available
                    if (emailJob.ExcelUploadId.HasValue && emailJob.ExcelUpload != null)
                    {
                        try
                        {
                            // Check cache first
                            if (!dynamicTableDataCache.TryGetValue(emailJob.ExcelUploadId.Value, out var tableData))
                            {
                                var tableName = GetTableNameFromExcelUpload(emailJob.ExcelUpload);
                                tableData = await dynamicTableService.GetDataFromDynamicTableAsync(tableName, 0, 1);
                                dynamicTableDataCache[emailJob.ExcelUploadId.Value] = tableData;
                            }

                            if (tableData.Any())
                            {
                                var dataDict = tableData.First();
                                ExtractEmployeeInfoFromData(dataDict, dto);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "⚠️ [EMAIL-MAPPING] Error getting dynamic table data for email job {EmailJobId}", emailJob.Id);
                        }
                    }

                    result.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [EMAIL-MAPPING] Error mapping email job {EmailJobId}", emailJob.Id);
                    // Add a basic DTO even if mapping fails
                    result.Add(new EmailJobDto
                    {
                        Id = emailJob.Id,
                        LetterTypeDefinitionId = emailJob.LetterTypeDefinitionId,
                        LetterTypeName = emailJob.LetterTypeDefinition?.DisplayName ?? string.Empty,
                        ExcelUploadId = emailJob.ExcelUploadId,
                        DocumentId = emailJob.DocumentId,
                        Subject = emailJob.Subject,
                        Content = emailJob.Content,
                        Status = emailJob.Status,
                        ErrorMessage = emailJob.ErrorMessage,
                        SendGridMessageId = emailJob.SendGridMessageId,
                        SentBy = emailJob.SentBy,
                        SentByName = emailJob.SentByUser?.Username ?? string.Empty,
                        SentAt = emailJob.SentAt,
                        CreatedAt = emailJob.CreatedAt,
                        UpdatedAt = emailJob.UpdatedAt,
                        RecipientEmail = emailJob.RecipientEmail,
                        RecipientName = emailJob.RecipientName
                    });
                }
            }

            _logger.LogInformation("✅ [EMAIL-MAPPING] Successfully mapped {Count} email jobs", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL-MAPPING] Error in batch mapping");
            throw;
        }
    }

    private void ExtractEmployeeInfoFromData(Dictionary<string, object> dataDict, EmailJobDto dto)
    {
        try
        {
            // Try common employee ID field names
            var idFields = new[] { "employee_id", "employeeId", "EmployeeId", "emp_id", "EmpId", "id", "Id" };
            foreach (var fieldName in idFields)
            {
                if (dataDict.TryGetValue(fieldName, out var idValue) && !string.IsNullOrEmpty(idValue?.ToString()))
                {
                    dto.EmployeeId = idValue.ToString()!;
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
                    dto.EmployeeName = nameValue.ToString()!;
                    break;
                }
            }

            // Try first_name and last_name separately
            if (string.IsNullOrEmpty(dto.EmployeeName))
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
                    dto.EmployeeName = $"{firstName} {lastName}".Trim();
                }
            }

            // Try common email field names
            var emailFields = new[] { "email", "Email", "email_address", "EmailAddress", "recipient_email", "employee_email" };
            foreach (var fieldName in emailFields)
            {
                if (dataDict.TryGetValue(fieldName, out var emailValue) && !string.IsNullOrEmpty(emailValue?.ToString()))
                {
                    dto.EmployeeEmail = emailValue.ToString()!;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [EMAIL-MAPPING] Error extracting employee info from data");
        }
    }

    private async Task<EmailJobDto> MapToEmailJobDtoAsync(EmailJob emailJob)
    {
        // Extract employee information from dynamic table
        var employeeId = string.Empty;
        var employeeName = string.Empty;
        var employeeEmail = string.Empty;

        try
        {
            if (emailJob.ExcelUpload != null)
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
            _logger.LogInformation("📧 [EMAIL-JOBS] Getting email jobs with page {Page}, pageSize {PageSize}", request.Page, request.PageSize);
            
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

            // Limit the page size to prevent large queries
            var pageSize = Math.Min(request.PageSize, 100);
            var skip = (request.Page - 1) * pageSize;

            _logger.LogInformation("📊 [EMAIL-JOBS] Querying {PageSize} records starting from {Skip}", pageSize, skip);

            var emailJobs = await query
                .Skip(skip)
                .Take(pageSize)
                .Include(e => e.LetterTypeDefinition)
                .Include(e => e.ExcelUpload)
                .Include(e => e.Document)
                .Include(e => e.SentByUser)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("✅ [EMAIL-JOBS] Retrieved {Count} email jobs from database", emailJobs.Count);

            // Optimize mapping by batching dynamic table queries with timeout
            var result = await ExecuteWithTimeoutAsync(
                () => MapEmailJobsToDtoBatchAsync(emailJobs),
                TimeSpan.FromSeconds(25), // 25 second timeout
                "Email jobs mapping"
            );

            _logger.LogInformation("✅ [EMAIL-JOBS] Successfully mapped {Count} email jobs to DTOs", result.Count());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL-JOBS] Error getting email jobs");
            throw;
        }
    }


    public async Task<string> RenderEmailContentAsync(string content, object data)
    {
        try
        {
            _logger.LogDebug("🎨 [EMAIL_RENDER] Starting email content rendering");
            
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

            _logger.LogDebug("✅ [EMAIL_RENDER] Email content rendering completed");
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL_RENDER] Error rendering email content");
            throw;
        }
    }

    public async Task<bool> ValidateEmailAddressAsync(string email)
    {
        try
        {
            _logger.LogDebug("📧 [EMAIL_VALIDATE] Validating email address: {Email}", email);
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogDebug("❌ [EMAIL_VALIDATE] Email is null or empty");
                return await Task.FromResult(false);
            }

            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            var isValid = emailRegex.IsMatch(email);
            
            _logger.LogDebug("✅ [EMAIL_VALIDATE] Email validation result: {IsValid}", isValid);
            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL_VALIDATE] Error validating email address");
            return await Task.FromResult(false);
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

    public async Task PollEmailStatusesAsync()
    {
        try
        {
            _logger.LogDebug("🔍 [EMAIL_POLLING] Starting email status polling at {Timestamp}", DateTime.UtcNow);

            // Get emails that are sent but not yet delivered, opened, clicked, bounced, or dropped
            // Also include emails that are pending but have been sent for more than 30 seconds (in case status update failed)
            var thirtySecondsAgo = DateTime.UtcNow.AddSeconds(-30);
            var emailsToCheck = await _dbContext.EmailJobs
                .Where(e => (e.Status == "sent" || (e.Status == "pending" && e.SentAt.HasValue && e.SentAt.Value < thirtySecondsAgo)) && 
                           e.SendGridMessageId != null && 
                           e.DeliveredAt == null && 
                           e.BouncedAt == null && 
                           e.DroppedAt == null)
                .Take(50) // Limit to 50 emails per polling cycle
                .ToListAsync();

            _logger.LogDebug("📊 [EMAIL_POLLING] Found {Count} emails to check for status updates", emailsToCheck.Count);

            if (!emailsToCheck.Any())
            {
                _logger.LogDebug("✅ [EMAIL_POLLING] No emails to check for status updates");
                return;
            }

            _logger.LogInformation("🔄 [EMAIL_POLLING] Checking status for {Count} emails", emailsToCheck.Count);

            var successCount = 0;
            var errorCount = 0;

            foreach (var emailJob in emailsToCheck)
            {
                try
                {
                    _logger.LogDebug("📧 [EMAIL_POLLING] Checking status for email job {EmailJobId} (SendGrid ID: {SendGridId})", 
                        emailJob.Id, emailJob.SendGridMessageId);
                    
                    await CheckEmailStatusWithSendGridAsync(emailJob);
                    successCount++;
                    
                    _logger.LogDebug("✅ [EMAIL_POLLING] Successfully checked status for email job {EmailJobId}", emailJob.Id);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "❌ [EMAIL_POLLING] Error checking status for email job {EmailJobId}: {Message}", 
                        emailJob.Id, ex.Message);
                }
            }

            _logger.LogInformation("📈 [EMAIL_POLLING] Polling completed - Success: {SuccessCount}, Errors: {ErrorCount}", 
                successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL_POLLING] Error in email status polling: {Message}", ex.Message);
        }
    }

    private async Task CheckEmailStatusWithSendGridAsync(EmailJob emailJob)
    {
        try
        {
            _logger.LogDebug("🔍 [SENDGRID_CHECK] Starting SendGrid Activity API check for email job {EmailJobId}", emailJob.Id);
            
            var apiKey = _configuration["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("⚠️ [SENDGRID_CHECK] SendGrid API key not configured for email job {EmailJobId}", emailJob.Id);
                return;
            }

            _logger.LogDebug("🔑 [SENDGRID_CHECK] API key found, creating SendGrid client for email job {EmailJobId}", emailJob.Id);
            var client = new SendGrid.SendGridClient(apiKey);
            
            // Use SendGrid Activity API to get events for this message
            var queryParams = new Dictionary<string, string>
            {
                { "query", $"sg_message_id=\"{emailJob.SendGridMessageId}\"" },
                { "limit", "10" }
            };

            _logger.LogDebug("📡 [SENDGRID_CHECK] Making Activity API request to SendGrid for message {MessageId}", emailJob.SendGridMessageId);
            
            // Build query string manually
            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var fullUrl = $"messages?{queryString}";
            
            var response = await client.RequestAsync(
                method: SendGrid.SendGridClient.Method.GET,
                urlPath: fullUrl
            );

            _logger.LogDebug("📊 [SENDGRID_CHECK] SendGrid Activity API response status: {StatusCode} for message {MessageId}", 
                response.StatusCode, emailJob.SendGridMessageId);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogDebug("📄 [SENDGRID_CHECK] Activity API response body length: {Length} for message {MessageId}", 
                    responseBody.Length, emailJob.SendGridMessageId);
                
                // Log the actual response for debugging
                _logger.LogDebug("📄 [SENDGRID_CHECK] Activity API response body: {ResponseBody}", responseBody);
                
                var activityData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                _logger.LogDebug("✅ [SENDGRID_CHECK] Successfully parsed SendGrid Activity API response for message {MessageId}", 
                    emailJob.SendGridMessageId);

                // Parse the response and update email status
                await UpdateEmailStatusFromActivityApiAsync(emailJob, activityData);
            }
            else
            {
                _logger.LogWarning("⚠️ [SENDGRID_CHECK] Failed to get message status from SendGrid Activity API for {MessageId}. Status: {StatusCode}", 
                    emailJob.SendGridMessageId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SENDGRID_CHECK] Error checking SendGrid Activity API status for email job {EmailJobId}: {Message}", 
                emailJob.Id, ex.Message);
        }
    }

    private async Task UpdateEmailStatusFromActivityApiAsync(EmailJob emailJob, JsonElement activityData)
    {
        try
        {
            _logger.LogDebug("🔄 [STATUS_UPDATE] Starting Activity API status update for email job {EmailJobId} with current status {CurrentStatus}", 
                emailJob.Id, emailJob.Status);
            
            var hasUpdates = false;
            var oldStatus = emailJob.Status;

            // The Activity API returns an array of events
            if (activityData.TryGetProperty("messages", out var messages) && 
                messages.ValueKind == JsonValueKind.Array)
            {
                _logger.LogDebug("📋 [STATUS_UPDATE] Found {Count} messages in Activity API response for email job {EmailJobId}", 
                    messages.GetArrayLength(), emailJob.Id);

                foreach (var message in messages.EnumerateArray())
                {
                    if (message.TryGetProperty("events", out var events) && 
                        events.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogDebug("📊 [STATUS_UPDATE] Found {Count} events for message in email job {EmailJobId}", 
                            events.GetArrayLength(), emailJob.Id);

                        foreach (var eventItem in events.EnumerateArray())
                        {
                            if (eventItem.TryGetProperty("event", out var eventType))
                            {
                                var eventTypeStr = eventType.GetString();
                                _logger.LogDebug("🎯 [STATUS_UPDATE] Processing event type: {EventType} for email job {EmailJobId}", 
                                    eventTypeStr, emailJob.Id);

                                switch (eventTypeStr?.ToLowerInvariant())
                                {
                                    case "delivered":
                                        if (emailJob.DeliveredAt == null && 
                                            eventItem.TryGetProperty("timestamp", out var deliveredTimestamp))
                                        {
                                            emailJob.Status = "delivered";
                                            emailJob.DeliveredAt = DateTimeOffset.FromUnixTimeSeconds(deliveredTimestamp.GetInt64()).DateTime;
                                            hasUpdates = true;
                                            _logger.LogInformation("✅ [STATUS_UPDATE] Updated to delivered status for email job {EmailJobId}", emailJob.Id);
                                        }
                                        break;

                                    case "open":
                                        if (emailJob.OpenedAt == null && 
                                            eventItem.TryGetProperty("timestamp", out var openedTimestamp))
                                        {
                                            emailJob.Status = "opened";
                                            emailJob.OpenedAt = DateTimeOffset.FromUnixTimeSeconds(openedTimestamp.GetInt64()).DateTime;
                                            hasUpdates = true;
                                            _logger.LogInformation("✅ [STATUS_UPDATE] Updated to opened status for email job {EmailJobId}", emailJob.Id);
                                        }
                                        break;

                                    case "click":
                                        if (emailJob.ClickedAt == null && 
                                            eventItem.TryGetProperty("timestamp", out var clickedTimestamp))
                                        {
                                            emailJob.Status = "clicked";
                                            emailJob.ClickedAt = DateTimeOffset.FromUnixTimeSeconds(clickedTimestamp.GetInt64()).DateTime;
                                            hasUpdates = true;
                                            _logger.LogInformation("✅ [STATUS_UPDATE] Updated to clicked status for email job {EmailJobId}", emailJob.Id);
                                        }
                                        break;

                                    case "bounce":
                                        if (emailJob.BouncedAt == null && 
                                            eventItem.TryGetProperty("timestamp", out var bouncedTimestamp))
                                        {
                                            emailJob.Status = "bounced";
                                            emailJob.BouncedAt = DateTimeOffset.FromUnixTimeSeconds(bouncedTimestamp.GetInt64()).DateTime;
                                            if (eventItem.TryGetProperty("reason", out var bounceReason))
                                            {
                                                emailJob.BounceReason = bounceReason.GetString();
                                            }
                                            hasUpdates = true;
                                            _logger.LogInformation("✅ [STATUS_UPDATE] Updated to bounced status for email job {EmailJobId}", emailJob.Id);
                                        }
                                        break;

                                    case "dropped":
                                        if (emailJob.DroppedAt == null && 
                                            eventItem.TryGetProperty("timestamp", out var droppedTimestamp))
                                        {
                                            emailJob.Status = "dropped";
                                            emailJob.DroppedAt = DateTimeOffset.FromUnixTimeSeconds(droppedTimestamp.GetInt64()).DateTime;
                                            if (eventItem.TryGetProperty("reason", out var dropReason))
                                            {
                                                emailJob.DropReason = dropReason.GetString();
                                            }
                                            hasUpdates = true;
                                            _logger.LogInformation("✅ [STATUS_UPDATE] Updated to dropped status for email job {EmailJobId}", emailJob.Id);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ [STATUS_UPDATE] No messages found in Activity API response for email job {EmailJobId}", emailJob.Id);
            }

            if (hasUpdates)
            {
                emailJob.UpdatedAt = DateTime.UtcNow;
                await _emailJobRepository.UpdateAsync(emailJob);
                
                _logger.LogInformation("📧 [STATUS_UPDATE] Email job {EmailJobId} status updated from {OldStatus} to {NewStatus}", 
                    emailJob.Id, oldStatus, emailJob.Status);

                // Send real-time notification
                await _realTimeService.NotifyEmailStatusUpdateAsync(
                    emailJob.SentBy.ToString(), 
                    new EmailStatusUpdate
                    {
                        EmailJobId = emailJob.Id,
                        Status = emailJob.Status,
                        Timestamp = emailJob.UpdatedAt,
                        EmployeeName = emailJob.RecipientName,
                        Reason = emailJob.BounceReason ?? emailJob.DropReason
                    }
                );
            }
            else
            {
                _logger.LogDebug("ℹ️ [STATUS_UPDATE] No status updates needed for email job {EmailJobId}", emailJob.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [STATUS_UPDATE] Error updating email status from Activity API for email job {EmailJobId}: {Message}", 
                emailJob.Id, ex.Message);
        }
    }

    private async Task<(string Name, string Email)> GetEmployeeInfoAsync(EmailJob emailJob)
    {
        try
        {
            _logger.LogDebug("👤 [EMPLOYEE_INFO] Getting employee info for email job {EmailJobId}", emailJob.Id);
            
            // Get the Excel upload to access the dynamic table data
            _logger.LogDebug("📊 [EMPLOYEE_INFO] Looking up Excel upload {ExcelUploadId} for email job {EmailJobId}", 
                emailJob.ExcelUploadId, emailJob.Id);
            
            var excelUpload = await _dbContext.ExcelUploads
                .FirstOrDefaultAsync(e => e.Id == emailJob.ExcelUploadId);

            if (excelUpload?.ParsedData == null)
            {
                _logger.LogDebug("⚠️ [EMPLOYEE_INFO] Excel upload or parsed data not found for email job {EmailJobId}, using fallback", emailJob.Id);
                return (emailJob.RecipientName ?? "Unknown", emailJob.RecipientEmail ?? "");
            }

            _logger.LogDebug("✅ [EMPLOYEE_INFO] Found Excel upload for email job {EmailJobId}, parsing data", emailJob.Id);

            // Parse the dynamic data
            var dataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(excelUpload.ParsedData);
            if (dataDict == null)
            {
                _logger.LogDebug("⚠️ [EMPLOYEE_INFO] Failed to parse Excel data for email job {EmailJobId}, using fallback", emailJob.Id);
                return (emailJob.RecipientName ?? "Unknown", emailJob.RecipientEmail ?? "");
            }

            _logger.LogDebug("📋 [EMPLOYEE_INFO] Successfully parsed Excel data with {Count} fields for email job {EmailJobId}", 
                dataDict.Count, emailJob.Id);

            // Extract employee name
            var employeeName = string.Empty;
            var employeeEmail = string.Empty;

            // Try to find name fields
            var nameFields = new[] { "name", "full_name", "employee_name", "first_name", "last_name" };
            _logger.LogDebug("🔍 [EMPLOYEE_INFO] Searching for name fields in data for email job {EmailJobId}", emailJob.Id);
            
            foreach (var fieldName in nameFields)
            {
                if (dataDict.TryGetValue(fieldName, out var nameValue) && !string.IsNullOrEmpty(nameValue?.ToString()))
                {
                    employeeName = nameValue.ToString()!;
                    _logger.LogDebug("✅ [EMPLOYEE_INFO] Found name field '{FieldName}' with value '{Value}' for email job {EmailJobId}", 
                        fieldName, employeeName, emailJob.Id);
                    break;
                }
            }

            // Try first_name and last_name separately
            if (string.IsNullOrEmpty(employeeName))
            {
                _logger.LogDebug("🔍 [EMPLOYEE_INFO] No single name field found, trying first_name and last_name for email job {EmailJobId}", emailJob.Id);
                
                var firstName = dataDict.TryGetValue("first_name", out var firstNameValue) ? firstNameValue?.ToString() : "";
                var lastName = dataDict.TryGetValue("last_name", out var lastNameValue) ? lastNameValue?.ToString() : "";
                
                if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                {
                    employeeName = $"{firstName} {lastName}".Trim();
                    _logger.LogDebug("✅ [EMPLOYEE_INFO] Combined first_name '{FirstName}' and last_name '{LastName}' to '{FullName}' for email job {EmailJobId}", 
                        firstName, lastName, employeeName, emailJob.Id);
                }
            }

            // Extract email
            var emailFields = new[] { "email", "email_address", "employee_email" };
            _logger.LogDebug("🔍 [EMPLOYEE_INFO] Searching for email fields in data for email job {EmailJobId}", emailJob.Id);
            
            foreach (var fieldName in emailFields)
            {
                if (dataDict.TryGetValue(fieldName, out var emailValue) && !string.IsNullOrEmpty(emailValue?.ToString()))
                {
                    employeeEmail = emailValue.ToString()!;
                    _logger.LogDebug("✅ [EMPLOYEE_INFO] Found email field '{FieldName}' with value '{Value}' for email job {EmailJobId}", 
                        fieldName, employeeEmail, emailJob.Id);
                    break;
                }
            }

            var resultName = string.IsNullOrEmpty(employeeName) ? emailJob.RecipientName ?? "Unknown" : employeeName;
            var resultEmail = string.IsNullOrEmpty(employeeEmail) ? emailJob.RecipientEmail ?? "" : employeeEmail;
            
            _logger.LogDebug("✅ [EMPLOYEE_INFO] Final employee info - Name: '{Name}', Email: '{Email}' for email job {EmailJobId}", 
                resultName, resultEmail, emailJob.Id);

            return (resultName, resultEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMPLOYEE_INFO] Error getting employee info for email job {EmailJobId}: {Message}", 
                emailJob.Id, ex.Message);
            return (emailJob.RecipientName ?? "Unknown", emailJob.RecipientEmail ?? "");
        }
    }

    public async Task<PaginatedResponse<EmailJobDto>> GetEmailHistoryAsync(string tabId, GetEmailHistoryRequest request)
    {
        try
        {
            var query = _dbContext.EmailJobs
                .Include(ej => ej.LetterTypeDefinition)
                .Include(ej => ej.SentByUser)
                .AsQueryable();

            // Filter by tab/letter type
            if (Guid.TryParse(tabId, out var letterTypeId))
            {
                query = query.Where(ej => ej.LetterTypeDefinitionId == letterTypeId);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(ej => ej.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(ej => 
                    (ej.RecipientName != null && ej.RecipientName.ToLower().Contains(searchTerm)) ||
                    (ej.RecipientEmail != null && ej.RecipientEmail.ToLower().Contains(searchTerm))
                );
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(ej => ej.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(ej => ej.CreatedAt <= request.ToDate.Value);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "createdat" => request.SortDirection == "asc" ? query.OrderBy(ej => ej.CreatedAt) : query.OrderByDescending(ej => ej.CreatedAt),
                "status" => request.SortDirection == "asc" ? query.OrderBy(ej => ej.Status) : query.OrderByDescending(ej => ej.Status),
                "recipientname" => request.SortDirection == "asc" ? query.OrderBy(ej => ej.RecipientName) : query.OrderByDescending(ej => ej.RecipientName),
                "recipientemail" => request.SortDirection == "asc" ? query.OrderBy(ej => ej.RecipientEmail) : query.OrderByDescending(ej => ej.RecipientEmail),
                _ => request.SortDirection == "asc" ? query.OrderBy(ej => ej.CreatedAt) : query.OrderByDescending(ej => ej.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var emailJobs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var emails = new List<EmailJobDto>();
            foreach (var job in emailJobs)
            {
                emails.Add(await MapToEmailJobDtoAsync(job));
            }

            return new PaginatedResponse<EmailJobDto>
            {
                Items = emails,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email history for tab: {TabId}", tabId);
            throw;
        }
    }

    public async Task<EmailStatsDto> GetEmailStatsAsync(string tabId)
    {
        try
        {
            var query = _dbContext.EmailJobs.AsQueryable();

            // Filter by tab/letter type
            if (Guid.TryParse(tabId, out var letterTypeId))
            {
                query = query.Where(ej => ej.LetterTypeDefinitionId == letterTypeId);
            }

            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var totalEmails = await query.CountAsync();
            var deliveredEmails = await query.CountAsync(ej => ej.Status == "delivered");
            var pendingEmails = await query.CountAsync(ej => ej.Status == "pending");
            var failedEmails = await query.CountAsync(ej => ej.Status == "bounced" || ej.Status == "dropped");
            var bouncedEmails = await query.CountAsync(ej => ej.Status == "bounced");
            var openedEmails = await query.CountAsync(ej => ej.Status == "opened");

            var emailsToday = await query.CountAsync(ej => ej.CreatedAt >= today);
            var emailsThisWeek = await query.CountAsync(ej => ej.CreatedAt >= weekStart);
            var emailsThisMonth = await query.CountAsync(ej => ej.CreatedAt >= monthStart);

            var successRate = totalEmails > 0 ? (double)deliveredEmails / totalEmails * 100 : 0;

            // Calculate average delivery time
            var deliveredEmailsWithTimes = await query
                .Where(ej => ej.Status == "delivered" && ej.SentAt.HasValue)
                .Select(ej => new { ej.CreatedAt, ej.SentAt })
                .ToListAsync();

            var averageDeliveryTime = deliveredEmailsWithTimes.Any() 
                ? deliveredEmailsWithTimes.Average(ej => (ej.SentAt!.Value - ej.CreatedAt).TotalMinutes)
                : 0;

            // Status distribution
            var statusDistribution = await query
                .GroupBy(ej => ej.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Hourly distribution (last 24 hours)
            var hourlyDistribution = await query
                .Where(ej => ej.CreatedAt >= now.AddHours(-24))
                .GroupBy(ej => ej.CreatedAt.Hour)
                .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count());

            // Daily distribution (last 30 days)
            var dailyDistribution = await query
                .Where(ej => ej.CreatedAt >= now.AddDays(-30))
                .GroupBy(ej => ej.CreatedAt.Date)
                .ToDictionaryAsync(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

            return new EmailStatsDto
            {
                TotalEmails = totalEmails,
                DeliveredEmails = deliveredEmails,
                PendingEmails = pendingEmails,
                FailedEmails = failedEmails,
                BouncedEmails = bouncedEmails,
                OpenedEmails = openedEmails,
                SuccessRate = successRate,
                AverageDeliveryTime = averageDeliveryTime,
                EmailsToday = emailsToday,
                EmailsThisWeek = emailsThisWeek,
                EmailsThisMonth = emailsThisMonth,
                StatusDistribution = statusDistribution,
                HourlyDistribution = hourlyDistribution,
                DailyDistribution = dailyDistribution
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email stats for tab: {TabId}", tabId);
            throw;
        }
    }

    public async Task<object> GetInsightsAsync(string tabId, string timeRange)
    {
        try
        {
            var query = _dbContext.EmailJobs.AsQueryable();

            // Filter by tab/letter type
            if (Guid.TryParse(tabId, out var letterTypeId))
            {
                query = query.Where(ej => ej.LetterTypeDefinitionId == letterTypeId);
            }

            // Apply time range filter
            var now = DateTime.UtcNow;
            var fromDate = timeRange switch
            {
                "7d" => now.AddDays(-7),
                "30d" => now.AddDays(-30),
                "90d" => now.AddDays(-90),
                "1y" => now.AddDays(-365),
                _ => now.AddDays(-30)
            };

            query = query.Where(ej => ej.CreatedAt >= fromDate);

            var emails = await query.ToListAsync();

            // Calculate insights
            var totalEmails = emails.Count;
            var deliveredEmails = emails.Count(ej => ej.Status == "delivered");
            var pendingEmails = emails.Count(ej => ej.Status == "pending");
            var failedEmails = emails.Count(ej => ej.Status == "bounced" || ej.Status == "dropped");
            var successRate = totalEmails > 0 ? (double)deliveredEmails / totalEmails * 100 : 0;

            // Calculate average delivery time
            var deliveredEmailsWithTimes = emails
                .Where(ej => ej.Status == "delivered" && ej.SentAt.HasValue)
                .ToList();

            var averageDeliveryTime = deliveredEmailsWithTimes.Any() 
                ? deliveredEmailsWithTimes.Average(ej => (ej.SentAt!.Value - ej.CreatedAt).TotalMinutes)
                : 0;

            // Daily stats
            var dailyStats = emails
                .GroupBy(ej => ej.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    sent = g.Count(),
                    delivered = g.Count(ej => ej.Status == "delivered"),
                    failed = g.Count(ej => ej.Status == "bounced" || ej.Status == "dropped")
                })
                .OrderBy(x => x.date)
                .ToList();

            // Hourly stats
            var hourlyStats = emails
                .GroupBy(ej => ej.CreatedAt.Hour)
                .Select(g => new
                {
                    hour = g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.hour)
                .ToList();

            // Status distribution
            var statusDistribution = emails
                .GroupBy(ej => ej.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count(),
                    percentage = totalEmails > 0 ? (double)g.Count() / totalEmails * 100 : 0
                })
                .ToList();

            // Top recipients
            var topRecipients = emails
                .GroupBy(ej => new { ej.RecipientEmail, ej.RecipientName })
                .Select(g => new
                {
                    email = g.Key.RecipientEmail,
                    name = g.Key.RecipientName,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            // Calculate trends
            var weeklyGrowth = 0.0;
            var monthlyGrowth = 0.0;
            var deliveryImprovement = 0.0;

            if (timeRange == "30d")
            {
                var firstWeek = emails.Where(ej => ej.CreatedAt >= now.AddDays(-7)).Count();
                var secondWeek = emails.Where(ej => ej.CreatedAt >= now.AddDays(-14) && ej.CreatedAt < now.AddDays(-7)).Count();
                weeklyGrowth = secondWeek > 0 ? (double)(firstWeek - secondWeek) / secondWeek * 100 : 0;

                var firstMonth = emails.Where(ej => ej.CreatedAt >= now.AddDays(-30)).Count();
                var secondMonth = emails.Where(ej => ej.CreatedAt >= now.AddDays(-60) && ej.CreatedAt < now.AddDays(-30)).Count();
                monthlyGrowth = secondMonth > 0 ? (double)(firstMonth - secondMonth) / secondMonth * 100 : 0;
            }

            return new
            {
                totalEmails,
                deliveredEmails,
                pendingEmails,
                failedEmails,
                successRate,
                averageDeliveryTime,
                dailyStats,
                hourlyStats,
                statusDistribution,
                topRecipients,
                trends = new
                {
                    weeklyGrowth,
                    monthlyGrowth,
                    deliveryImprovement
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights for tab: {TabId}", tabId);
            throw;
        }
    }

    public async Task<object> GetAnalyticsAsync(string tabId, string timeRange, string metric)
    {
        try
        {
            // This would be implemented based on specific analytics requirements
            // For now, return basic analytics
            var insights = await GetInsightsAsync(tabId, timeRange);
            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for tab: {TabId}", tabId);
            throw;
        }
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            _logger.LogDebug("⏱️ [TIMEOUT-WRAPPER] Starting {OperationName} with timeout {Timeout}ms", operationName, timeout.TotalMilliseconds);
            
            var task = operation();
            var result = await task.WaitAsync(cts.Token);
            
            _logger.LogDebug("✅ [TIMEOUT-WRAPPER] {OperationName} completed successfully", operationName);
            return result;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            _logger.LogWarning("⏰ [TIMEOUT-WRAPPER] {OperationName} timed out after {Timeout}ms", operationName, timeout.TotalMilliseconds);
            throw new TimeoutException($"Operation '{operationName}' timed out after {timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TIMEOUT-WRAPPER] Error in {OperationName}: {Message}", operationName, ex.Message);
            throw;
        }
    }
}
