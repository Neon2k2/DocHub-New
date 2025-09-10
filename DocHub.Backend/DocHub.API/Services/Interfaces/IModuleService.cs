using DocHub.API.DTOs;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface IModuleService
{
    Task<List<ModuleSummary>> GetModulesAsync();
    Task<ModuleDetail> GetModuleAsync(Guid moduleId);
    Task<ModuleSummary> CreateModuleAsync(CreateModuleRequest request);
    Task<ModuleSummary> UpdateModuleAsync(Guid moduleId, UpdateModuleRequest request);
    Task DeleteModuleAsync(Guid moduleId);
    Task<ModuleStatistics> GetModuleStatisticsAsync(Guid moduleId);
    Task<List<LetterTypeSummary>> GetModuleLetterTypesAsync(Guid moduleId);
    Task<List<ActivitySummary>> GetModuleActivitiesAsync(Guid moduleId, int limit = 10);
    Task<ModuleDashboard> GetModuleDashboardAsync(Guid moduleId);
}
