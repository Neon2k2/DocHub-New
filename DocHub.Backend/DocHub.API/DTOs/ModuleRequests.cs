using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class ModuleSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ModuleDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateModuleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateModuleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ModuleStatistics
{
    public Guid ModuleId { get; set; }
    public int LetterTypesCount { get; set; }
    public int TemplatesCount { get; set; }
    public int SignaturesCount { get; set; }
    public int GeneratedDocumentsCount { get; set; }
    public int EmailJobsCount { get; set; }
}

public class LetterTypeSummary
{
    public Guid Id { get; set; }
    public string TypeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivitySummary
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ModuleDashboard
{
    public ModuleSummary Module { get; set; } = new();
    public ModuleStatistics Statistics { get; set; } = new();
    public List<ActivitySummary> RecentActivities { get; set; } = new();
    public List<LetterTypeSummary> LetterTypes { get; set; } = new();
}
