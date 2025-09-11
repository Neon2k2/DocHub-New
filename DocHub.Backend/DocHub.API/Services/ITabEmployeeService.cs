using DocHub.API.Models;
using DocHub.API.DTOs;

namespace DocHub.API.Services.Interfaces;

public interface ITabEmployeeService
{
    Task<PagedResult<TabEmployeeData>> GetEmployeesForTabAsync(Guid tabId, int page = 1, int limit = 50, string? search = null, string? department = null, string? status = null);
    Task<TabEmployeeData?> GetEmployeeByIdAsync(Guid id);
    Task<TabEmployeeData> CreateEmployeeAsync(TabEmployeeData employee);
    Task<TabEmployeeData> UpdateEmployeeAsync(TabEmployeeData employee);
    Task<bool> DeleteEmployeeAsync(Guid id);
    Task<bool> ImportFromExcelAsync(Guid tabId, IFormFile excelFile, Dictionary<string, string> columnMappings);
    Task<List<TabEmployeeData>> GetEmployeesByTabIdAsync(Guid tabId);
    Task<bool> ClearTabDataAsync(Guid tabId);
}
