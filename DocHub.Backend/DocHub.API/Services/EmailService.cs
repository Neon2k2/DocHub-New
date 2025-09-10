using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DocHub.API.Services;

public class EmailService : IEmailService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(DocHubDbContext context, ILogger<EmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BulkEmailResult> SendBulkAsync(BulkEmailRequest request)
    {
        try
        {
            var jobId = Guid.NewGuid();
            var emailJobs = new List<EmailJob>();

            foreach (var employeeId in request.EmployeeIds)
            {
                var emailJob = new EmailJob
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId.ToString(),
                    DocumentId = request.DocumentId,
                    EmailTemplateId = request.EmailTemplateId,
                    Subject = request.Subject,
                    Content = request.Content,
                    Attachments = request.Attachments?.ToJson() ?? "[]",
                    Status = "Pending",
                    SentBy = request.SentBy,
                    CreatedAt = DateTime.UtcNow,
                    TrackingId = $"{jobId}_{employeeId}"
                };

                emailJobs.Add(emailJob);
            }

            _context.EmailJobs.AddRange(emailJobs);
            await _context.SaveChangesAsync();

            // TODO: Implement actual email sending logic with SendGrid
            // For now, just mark as sent
            foreach (var job in emailJobs)
            {
                job.Status = "Sent";
                job.SentAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new BulkEmailResult
            {
                Success = true,
                JobId = jobId,
                TotalEmails = emailJobs.Count,
                SentEmails = emailJobs.Count,
                FailedEmails = 0,
                Message = "Bulk emails sent successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk emails");
            return new BulkEmailResult
            {
                Success = false,
                Message = "Failed to send bulk emails"
            };
        }
    }

    public async Task<EmailResult> SendSingleAsync(SingleEmailRequest request)
    {
        try
        {
            var emailJob = new EmailJob
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId.ToString(),
                DocumentId = request.DocumentId,
                EmailTemplateId = request.EmailTemplateId,
                Subject = request.Subject,
                Content = request.Content,
                Attachments = request.Attachments?.ToJson() ?? "[]",
                Status = "Pending",
                SentBy = request.SentBy,
                CreatedAt = DateTime.UtcNow,
                TrackingId = Guid.NewGuid().ToString()
            };

            _context.EmailJobs.Add(emailJob);
            await _context.SaveChangesAsync();

            // TODO: Implement actual email sending logic with SendGrid
            // For now, just mark as sent
            emailJob.Status = "Sent";
            emailJob.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new EmailResult
            {
                Success = true,
                JobId = emailJob.Id,
                Message = "Email sent successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send single email");
            return new EmailResult
            {
                Success = false,
                Message = "Failed to send email"
            };
        }
    }

    public async Task<EmailJobStatus> GetJobStatusAsync(Guid jobId)
    {
        var job = await _context.EmailJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            throw new ArgumentException("Email job not found");
        }

        return new EmailJobStatus
        {
            JobId = job.Id,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            SentAt = job.SentAt,
            TrackingId = job.TrackingId ?? string.Empty
        };
    }

    public async Task<PagedResult<EmailJobSummary>> GetJobsAsync(int page, int pageSize, string? status = null, string? search = null)
    {
        var query = _context.EmailJobs.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j => j.Subject.Contains(search) || j.Content.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new EmailJobSummary
            {
                Id = j.Id,
                EmployeeId = Guid.Parse(j.EmployeeId),
                Subject = j.Subject,
                Status = j.Status,
                CreatedAt = j.CreatedAt,
                SentAt = j.SentAt
            })
            .ToListAsync();

        return new PagedResult<EmailJobSummary>
        {
            Items = jobs,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task CancelJobAsync(Guid jobId)
    {
        var job = await _context.EmailJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            throw new ArgumentException("Email job not found");
        }

        if (job.Status == "Sent")
        {
            throw new InvalidOperationException("Cannot cancel a sent email");
        }

        job.Status = "Cancelled";
        await _context.SaveChangesAsync();
    }

    public async Task RetryJobAsync(Guid jobId)
    {
        var job = await _context.EmailJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            throw new ArgumentException("Email job not found");
        }

        if (job.Status != "Failed")
        {
            throw new InvalidOperationException("Can only retry failed jobs");
        }

        job.Status = "Pending";
        job.SentAt = null;
        await _context.SaveChangesAsync();

        // TODO: Implement actual retry logic
    }

    public async Task<List<EmailTemplateSummary>> GetTemplatesAsync()
    {
        var templates = await _context.EmailTemplates
            .Where(t => t.IsActive)
            .Select(t => new EmailTemplateSummary
            {
                Id = t.Id,
                Name = t.Name,
                Subject = t.Subject,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return templates;
    }

    public async Task<EmailTemplateSummary> CreateTemplateAsync(CreateEmailTemplateRequest request)
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Content,
            IsActive = true,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        return new EmailTemplateSummary
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt
        };
    }

    public async Task<EmailTemplateSummary> UpdateTemplateAsync(Guid templateId, UpdateEmailTemplateRequest request)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Email template not found");
        }

        template.Name = request.Name;
        template.Subject = request.Subject;
        template.Body = request.Content;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new EmailTemplateSummary
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt
        };
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException("Email template not found");
        }

        _context.EmailTemplates.Remove(template);
        await _context.SaveChangesAsync();
    }
}
