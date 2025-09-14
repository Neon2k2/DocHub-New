using DocHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocHub.Core.Interfaces;

public interface IDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<LetterTypeDefinition> LetterTypeDefinitions { get; }
    DbSet<DynamicField> DynamicFields { get; }
    DbSet<FileReference> FileReferences { get; }
    DbSet<DocumentTemplate> DocumentTemplates { get; }
    DbSet<Signature> Signatures { get; }
    DbSet<ExcelUpload> ExcelUploads { get; }
    DbSet<GeneratedDocument> GeneratedDocuments { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailJob> EmailJobs { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<TableSchema> TableSchemas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
    int SaveChanges();
    int SaveChanges(bool acceptAllChangesOnSuccess);
}
