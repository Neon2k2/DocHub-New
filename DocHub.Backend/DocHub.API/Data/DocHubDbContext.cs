using Microsoft.EntityFrameworkCore;
using DocHub.API.Models;
using DocHub.API.Extensions;

namespace DocHub.API.Data;

public class DocHubDbContext : DbContext
{
    public DocHubDbContext(DbContextOptions<DocHubDbContext> options) : base(options)
    {
    }

    // Core Tables
    public DbSet<Module> Modules { get; set; }
    public DbSet<LetterTypeDefinition> LetterTypeDefinitions { get; set; }
    
    // Document Management
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
    public DbSet<Signature> Signatures { get; set; }
    public DbSet<GeneratedDocument> GeneratedDocuments { get; set; }
    
    // Email System
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailJob> EmailJobs { get; set; }
    public DbSet<EmailEvent> EmailEvents { get; set; }
    
    // Excel Processing
    public DbSet<ExcelUpload> ExcelUploads { get; set; }
    
    // Employee Management
    public DbSet<Employee> Employees { get; set; }
    public DbSet<TabEmployeeData> TabEmployeeData { get; set; }
    
    // Audit & Logging
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    
    // File Management
    public DbSet<FileStorage> FileStorage { get; set; }
    
    // Authentication & Authorization
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Module configuration
        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // LetterTypeDefinition configuration
        modelBuilder.Entity<LetterTypeDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TypeKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FieldConfiguration).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.TypeKey).IsUnique();
            
            entity.HasOne(e => e.Module)
                  .WithMany(e => e.LetterTypeDefinitions)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });


        // DocumentTemplate configuration
        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Placeholders).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Module)
                  .WithMany(e => e.DocumentTemplates)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Signature configuration
        modelBuilder.Entity<Signature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Module)
                  .WithMany(e => e.Signatures)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // GeneratedDocument configuration
        modelBuilder.Entity<GeneratedDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.DownloadUrl).HasMaxLength(1000);
            entity.Property(e => e.GeneratedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.LetterTypeDefinition)
                  .WithMany()
                  .HasForeignKey(e => e.LetterTypeDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Module)
                  .WithMany(e => e.GeneratedDocuments)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // EmailTemplate configuration
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Body).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.HtmlBody).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Placeholders).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        });

        // EmailJob configuration
        modelBuilder.Entity<EmailJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EmployeeName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EmployeeEmail).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SentBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TrackingId).HasMaxLength(100);
            entity.Property(e => e.SendGridMessageId).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.LetterTypeDefinition)
                  .WithMany()
                  .HasForeignKey(e => e.LetterTypeDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Module)
                  .WithMany(e => e.EmailJobs)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // EmailEvent configuration
        modelBuilder.Entity<EmailEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Data).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.EmailJob)
                  .WithMany(e => e.Events)
                  .HasForeignKey(e => e.EmailJobId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ExcelUpload configuration
        modelBuilder.Entity<ExcelUpload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ProcessedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FieldMappings).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProcessingOptions).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Results).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.LetterTypeDefinition)
                  .WithMany()
                  .HasForeignKey(e => e.LetterTypeDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Manager).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.IsActive).IsRequired();
            
            entity.HasIndex(e => e.EmployeeId).IsUnique();
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OldValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500);
        });

        // WebhookEvent configuration
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailJobId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Response).HasMaxLength(1000);
            entity.Property(e => e.SgEventId).HasMaxLength(100);
            entity.Property(e => e.SgMessageId).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.UniqueArgs).HasMaxLength(1000);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Ip).HasMaxLength(45);
            entity.HasIndex(e => e.EmailJobId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
        });

        // FileStorage configuration
        modelBuilder.Entity<FileStorage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UploadedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModuleAccess).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        // TabEmployeeData configuration
        modelBuilder.Entity<TabEmployeeData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EmployeeName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.CustomFields).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DataSource).HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired();
            
            entity.HasOne(e => e.Tab)
                .WithMany()
                .HasForeignKey(e => e.TabId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.TabId, e.EmployeeId }).IsUnique();
        });

        // ExcelUpload configuration
        modelBuilder.Entity<ExcelUpload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(50);
            entity.Property(e => e.UploadedBy).HasMaxLength(100);
            entity.Property(e => e.ProcessedBy).HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ParsedData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.FieldMappings).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProcessingOptions).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Results).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsProcessed).IsRequired();
            entity.Property(e => e.ProcessedRows).IsRequired();
            
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany()
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
