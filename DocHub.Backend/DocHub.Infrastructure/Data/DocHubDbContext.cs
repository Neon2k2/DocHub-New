using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocHub.Infrastructure.Data;

public class DocHubDbContext : DbContext, IDbContext
{
    public DocHubDbContext(DbContextOptions<DocHubDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<LetterTypeDefinition> LetterTypeDefinitions => Set<LetterTypeDefinition>();
    public DbSet<DynamicField> DynamicFields => Set<DynamicField>();
    public DbSet<FileReference> FileReferences => Set<FileReference>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<ExcelUpload> ExcelUploads => Set<ExcelUpload>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();
    public DbSet<EmailJob> EmailJobs => Set<EmailJob>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<TableSchema> TableSchemas => Set<TableSchema>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for dashboard queries
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Users_IsActive");
            entity.HasIndex(e => e.LastLoginAt).HasDatabaseName("IX_Users_LastLoginAt");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Users_CreatedAt");
            entity.HasIndex(e => e.UpdatedAt).HasDatabaseName("IX_Users_UpdatedAt");
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });


        // Configure LetterTypeDefinition entity
        modelBuilder.Entity<LetterTypeDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TypeKey).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure DynamicField entity
        modelBuilder.Entity<DynamicField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany(e => e.DynamicFields)
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure FileReference entity
        modelBuilder.Entity<FileReference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.UploadedByUser)
                .WithMany(e => e.FileReferences)
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure DocumentTemplate entity
        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.File)
                .WithMany(e => e.DocumentTemplates)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure Signature entity
        modelBuilder.Entity<Signature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.File)
                .WithMany(e => e.Signatures)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });


        // Configure ExcelUpload entity
        modelBuilder.Entity<ExcelUpload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany(e => e.ExcelUploads)
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.File)
                .WithMany(e => e.ExcelUploads)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ProcessedByUser)
                .WithMany(e => e.ExcelUploads)
                .HasForeignKey(e => e.ProcessedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure GeneratedDocument entity
        modelBuilder.Entity<GeneratedDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany(e => e.GeneratedDocuments)
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ExcelUpload)
                .WithMany()
                .HasForeignKey(e => e.ExcelUploadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Template)
                .WithMany(e => e.GeneratedDocuments)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Signature)
                .WithMany(e => e.GeneratedDocuments)
                .HasForeignKey(e => e.SignatureId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from SetNull to Restrict
            entity.HasOne(e => e.File)
                .WithMany(e => e.GeneratedDocuments)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.GeneratedByUser)
                .WithMany(e => e.GeneratedDocuments)
                .HasForeignKey(e => e.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for dashboard queries
            entity.HasIndex(e => e.GeneratedAt).HasDatabaseName("IX_GeneratedDocuments_GeneratedAt");
            entity.HasIndex(e => e.GeneratedBy).HasDatabaseName("IX_GeneratedDocuments_GeneratedBy");
            entity.HasIndex(e => e.ExcelUploadId).HasDatabaseName("IX_GeneratedDocuments_ExcelUploadId");
            entity.HasIndex(e => new { e.GeneratedAt, e.GeneratedBy }).HasDatabaseName("IX_GeneratedDocuments_GeneratedAt_GeneratedBy");
        });


        // Configure EmailJob entity
        modelBuilder.Entity<EmailJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany(e => e.EmailJobs)
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ExcelUpload)
                .WithMany()
                .HasForeignKey(e => e.ExcelUploadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Document)
                .WithMany(e => e.EmailJobs)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SentByUser)
                .WithMany(e => e.EmailJobs)
                .HasForeignKey(e => e.SentBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for dashboard queries
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_EmailJobs_Status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_EmailJobs_CreatedAt");
            entity.HasIndex(e => e.SentBy).HasDatabaseName("IX_EmailJobs_SentBy");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_EmailJobs_DocumentId");
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_EmailJobs_Status_CreatedAt");
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure WebhookEvent entity
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure TableSchema entity
        modelBuilder.Entity<TableSchema>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TableName).IsUnique();
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany()
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ExcelUpload)
                .WithMany()
                .HasForeignKey(e => e.ExcelUploadId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure UserSession entity
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IpAddress);
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // Configure RolePermission entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Role)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        });

        // Configure EmailTemplate entity
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LetterTypeDefinition)
                .WithMany()
                .HasForeignKey(e => e.LetterTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.LetterTypeDefinitionId).IsUnique();
        });

    }
}
