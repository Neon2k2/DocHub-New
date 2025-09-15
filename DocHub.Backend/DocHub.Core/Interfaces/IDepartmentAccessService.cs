using DocHub.Shared.DTOs.Users;

namespace DocHub.Core.Interfaces;

public interface IDepartmentAccessService
{
    Task<bool> UserCanAccessDepartment(Guid userId, string department);
    Task<bool> UserCanAccessTab(Guid userId, Guid tabId);
    Task<List<Guid>> GetAccessibleTabIds(Guid userId);
    Task<List<string>> GetUserDepartments(Guid userId);
    Task<bool> IsUserInDepartment(Guid userId, string department);
    Task<List<TabAccessDto>> GetUserTabAccess(Guid userId);
}

public class TabAccessDto
{
    public Guid TabId { get; set; }
    public string TabName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
