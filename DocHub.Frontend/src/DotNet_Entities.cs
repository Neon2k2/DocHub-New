// DocHub .NET Entity Models
// Add these to your Entity Framework DbContext

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocHub.Models
{
    // =============================================
    // 1. User Management & Authentication
    // =============================================

    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }
        
        [Required]
        public UserRole Role { get; set; }
        
        [StringLength(50)]
        public string? Department { get; set; }
        
        [StringLength(20)]
        public string? EmployeeId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLogin { get; set; }
        
        // Navigation Properties
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        
        // Computed Properties
        [NotMapped]
        public UserPermissions Permissions => new UserPermissions
        {
            CanAccessER = Role == UserRole.Admin || Role == UserRole.ER,
            CanAccessBilling = Role == UserRole.Admin || Role == UserRole.Billing,
            IsAdmin = Role == UserRole.Admin
        };
    }

    public enum UserRole
    {
        Admin,
        ER,
        Billing
    }

    public class UserPermissions
    {
        public bool CanAccessER { get; set; }
        public bool CanAccessBilling { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class RefreshToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Token { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRevoked { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    // =============================================
    // 2. Employee Management
    // =============================================

    public class Employee
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(20)]
        public string EmployeeId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [StringLength(15)]
        public string? Phone { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Department { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Designation { get; set; }
        
        public DateTime JoiningDate { get; set; }
        
        public DateTime? RelievingDate { get; set; }
        
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
        
        [StringLength(100)]
        public string? Manager { get; set; }
        
        [StringLength(50)]
        public string? Location { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Salary { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual EmployeeAddress? Address { get; set; }
        public virtual ICollection<ExperienceLetter> ExperienceLetters { get; set; } = new List<ExperienceLetter>();
        public virtual ICollection<TransferLetter> TransferLetters { get; set; } = new List<TransferLetter>();
        public virtual ICollection<HCLTimesheet> HCLTimesheets { get; set; } = new List<HCLTimesheet>();
    }

    public enum EmployeeStatus
    {
        Active,
        Inactive,
        Relieved
    }

    public class EmployeeAddress
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string EmployeeId { get; set; }
        
        [StringLength(200)]
        public string? Street { get; set; }
        
        [StringLength(50)]
        public string? City { get; set; }
        
        [StringLength(50)]
        public string? State { get; set; }
        
        [StringLength(10)]
        public string? Pincode { get; set; }
        
        [StringLength(50)]
        public string? Country { get; set; } = "India";
        
        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
    }

    // =============================================
    // 3. Experience Letters
    // =============================================

    public class ExperienceLetter
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string EmployeeId { get; set; }
        
        [Required]
        public string RequestedBy { get; set; }
        
        public LetterStatus Status { get; set; } = LetterStatus.Draft;
        
        public DateTime JoiningDate { get; set; }
        
        public DateTime RelievingDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Designation { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Department { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Salary { get; set; }
        
        public PerformanceRating Performance { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
        
        [StringLength(2000)]
        public string? CustomText { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ApprovedAt { get; set; }
        
        public string? ApprovedBy { get; set; }
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        [StringLength(500)]
        public string? GeneratedFilePath { get; set; }
        
        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
        
        [ForeignKey("RequestedBy")]
        public virtual User RequestedByUser { get; set; }
        
        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }
    }

    // =============================================
    // 4. Transfer Letters
    // =============================================

    public class TransferLetter
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string EmployeeId { get; set; }
        
        [Required]
        public string RequestedBy { get; set; }
        
        public LetterStatus Status { get; set; } = LetterStatus.Draft;
        
        [Required]
        [StringLength(50)]
        public string FromLocation { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ToLocation { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FromDepartment { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ToDepartment { get; set; }
        
        public DateTime EffectiveDate { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
        
        [StringLength(50)]
        public string? NewDesignation { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? NewSalary { get; set; }
        
        [StringLength(100)]
        public string? ReportingManager { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ApprovedAt { get; set; }
        
        public string? ApprovedBy { get; set; }
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        [StringLength(500)]
        public string? GeneratedFilePath { get; set; }
        
        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
        
        [ForeignKey("RequestedBy")]
        public virtual User RequestedByUser { get; set; }
        
        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }
    }

    public enum LetterStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected
    }

    public enum PerformanceRating
    {
        Excellent,
        Good,
        Satisfactory,
        NeedsImprovement
    }

    // =============================================
    // 5. Project Management
    // =============================================

    public class Project
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Client { get; set; }
        
        [Required]
        [StringLength(20)]
        public string ProjectCode { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal BillableRate { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual ICollection<HCLTimesheet> HCLTimesheets { get; set; } = new List<HCLTimesheet>();
        public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
    }

    public enum ProjectStatus
    {
        Active,
        Inactive,
        Completed,
        OnHold
    }

    public class ProjectAssignment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string ProjectId { get; set; }
        
        [Required]
        public string EmployeeId { get; set; }
        
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? UnassignedDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation Properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
    }

    // =============================================
    // 6. HCL Timesheet Management
    // =============================================

    public class HCLTimesheet
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string EmployeeId { get; set; }
        
        public DateTime WeekStartDate { get; set; }
        
        public DateTime WeekEndDate { get; set; }
        
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
        
        [Column(TypeName = "decimal(4,2)")]
        public decimal TotalHours { get; set; }
        
        [Column(TypeName = "decimal(4,2)")]
        public decimal BillableHours { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? SubmittedAt { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        public string? ApprovedBy { get; set; }
        
        [StringLength(500)]
        public string? Comments { get; set; }
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
        
        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }
        
        public virtual ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();
    }

    public class TimesheetEntry
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string TimesheetId { get; set; }
        
        public DateTime Date { get; set; }
        
        [Required]
        [StringLength(20)]
        public string ProjectCode { get; set; }
        
        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; }
        
        [Column(TypeName = "decimal(4,2)")]
        public decimal Hours { get; set; }
        
        public bool IsBillable { get; set; } = true;
        
        [StringLength(50)]
        public string? TaskType { get; set; }
        
        // Navigation Properties
        [ForeignKey("TimesheetId")]
        public virtual HCLTimesheet Timesheet { get; set; }
    }

    public enum TimesheetStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected
    }

    // =============================================
    // 7. System Management
    // =============================================

    public class SystemSetting
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string Key { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Value { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public string UpdatedBy { get; set; }
        
        // Navigation Properties
        [ForeignKey("UpdatedBy")]
        public virtual User UpdatedByUser { get; set; }
    }

    public class AuditLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; }
        
        [Required]
        [StringLength(100)]
        public string EntityType { get; set; }
        
        [StringLength(50)]
        public string? EntityId { get; set; }
        
        [StringLength(2000)]
        public string? Details { get; set; }
        
        [StringLength(45)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    // =============================================
    // 8. File Management
    // =============================================

    public class FileUpload
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; }
        
        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; }
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }
        
        [Required]
        [StringLength(100)]
        public string MimeType { get; set; }
        
        public long FileSize { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        [Required]
        public string UploadedBy { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; set; } = false;
        
        // Navigation Properties
        [ForeignKey("UploadedBy")]
        public virtual User UploadedByUser { get; set; }
    }

    // =============================================
    // 9. Email & Notification Management
    // =============================================


    public class EmailLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(200)]
        public string ToEmail { get; set; }
        
        [StringLength(200)]
        public string? CcEmail { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Subject { get; set; }
        
        [Required]
        public string Body { get; set; }
        
        public EmailStatus Status { get; set; } = EmailStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? SentAt { get; set; }
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        public int RetryCount { get; set; } = 0;
        
        public string? SentBy { get; set; }
        
        // Navigation Properties
        [ForeignKey("SentBy")]
        public virtual User? SentByUser { get; set; }
    }

    public enum EmailStatus
    {
        Pending,
        Sent,
        Failed,
        Cancelled
    }
}

// =============================================
// 10. DbContext Configuration
// =============================================

using Microsoft.EntityFrameworkCore;

namespace DocHub.Data
{
    public class DocHubDbContext : DbContext
    {
        public DocHubDbContext(DbContextOptions<DocHubDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
        public DbSet<ExperienceLetter> ExperienceLetters { get; set; }
        public DbSet<TransferLetter> TransferLetters { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }
        public DbSet<HCLTimesheet> HCLTimesheets { get; set; }
        public DbSet<TimesheetEntry> TimesheetEntries { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FileUpload> FileUploads { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeId)
                .IsUnique();

            modelBuilder.Entity<Project>()
                .HasIndex(p => p.ProjectCode)
                .IsUnique();

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => s.Key)
                .IsUnique();

            // Configure cascade delete behavior
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeAddress>()
                .HasOne(ea => ea.Employee)
                .WithOne(e => e.Address)
                .HasForeignKey<EmployeeAddress>(ea => ea.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = "admin-001",
                    Username = "admin",
                    Name = "System Administrator",
                    Email = "admin@dochub.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = "er-001",
                    Username = "er_user",
                    Name = "HR Manager",
                    Email = "hr@dochub.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo"),
                    Role = UserRole.ER,
                    Department = "Human Resources",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = "billing-001",
                    Username = "billing_user",
                    Name = "Finance Manager",
                    Email = "finance@dochub.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo"),
                    Role = UserRole.Billing,
                    Department = "Finance",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }

    // =============================================
    // 8. User Management & Session Management
    // =============================================

    public class UserSession
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string SessionToken { get; set; }
        
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(45)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime ExpiresAt { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    public class RefreshToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Token { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? RevokedAt { get; set; }
        
        [StringLength(500)]
        public string? ReplacedByToken { get; set; }
        
        [StringLength(100)]
        public string? ReasonRevoked { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    public class Permission
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string RoleId { get; set; }
        
        [Required]
        public string PermissionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }
        
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; }
    }

    public class UserRole
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string RoleId { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public string? AssignedBy { get; set; }
        
        public bool IsExpired { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }
        
        [ForeignKey("AssignedBy")]
        public virtual User? AssignedByUser { get; set; }
    }

    public class AuditLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string EntityName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; }
        
        [Required]
        public string EntityId { get; set; }
        
        [StringLength(100)]
        public string? UserId { get; set; }
        
        [StringLength(100)]
        public string? UserName { get; set; }
        
        [StringLength(45)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; } // JSON
        
        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; } // JSON
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Details { get; set; } // Additional details
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}