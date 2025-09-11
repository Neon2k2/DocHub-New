using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DocHub.API.DTOs;
using DocHub.API.Services.Interfaces;
using DocHub.API.Models;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/er/employees")]
[Authorize] // Re-enabled for testing
public class EmployeeController : ControllerBase
{
    private readonly ILogger<EmployeeController> _logger;
    private readonly ITabEmployeeService _tabEmployeeService;

    public EmployeeController(ILogger<EmployeeController> logger, ITabEmployeeService tabEmployeeService)
    {
        _logger = logger;
        _tabEmployeeService = tabEmployeeService;
    }

    /// <summary>
    /// Get all employees with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<Employee>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? department = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? tabId = null)
    {
        try
        {
            _logger.LogInformation("EmployeeController.GetAll called for tabId: {TabId}", tabId);
            
            if (!tabId.HasValue)
            {
                return BadRequest(new ApiResponse<PagedResult<Employee>>
                {
                    Success = false,
                    Data = null,
                    Error = new ApiError { Message = "TabId is required" }
                });
            }

            // Get tab-specific employee data
            var tabEmployeeData = await _tabEmployeeService.GetEmployeesForTabAsync(
                tabId.Value, page, limit, search, department, status);

            // Convert TabEmployeeData to Employee format for frontend compatibility
            var employees = tabEmployeeData.Items.Select(emp => new Employee
            {
                Id = emp.Id,
                EmployeeId = emp.EmployeeId,
                FirstName = emp.EmployeeName.Split(' ').FirstOrDefault() ?? "",
                LastName = emp.EmployeeName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Email = emp.Email ?? "",
                Phone = emp.Phone ?? "",
                Department = emp.Department ?? "",
                Position = emp.Position ?? "",
                IsActive = emp.IsActive,
                Manager = "", // Not available in TabEmployeeData
                Location = "", // Not available in TabEmployeeData
                Salary = null, // Not available in TabEmployeeData
                JoiningDate = DateTime.MinValue, // Not available in TabEmployeeData
                RelievingDate = null,
                CreatedAt = emp.CreatedAt,
                UpdatedAt = emp.UpdatedAt
            }).ToList();

            var paginatedResponse = new PagedResult<Employee>
            {
                Items = employees,
                TotalCount = tabEmployeeData.TotalCount,
                PageNumber = tabEmployeeData.PageNumber,
                PageSize = tabEmployeeData.PageSize
            };

            return Ok(new ApiResponse<PagedResult<Employee>>
            {
                Success = true,
                Data = paginatedResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get employees");
            return StatusCode(500, new ApiResponse<PagedResult<Employee>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<ApiResponse<Employee>> GetById(Guid id)
    {
        try
        {
            // Mock implementation - in real app, fetch from database
            var mockEmployee = new Employee
            {
                Id = id,
                EmployeeId = "EMP001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                Department = "Engineering",
                Position = "Software Developer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(new ApiResponse<Employee>
            {
                Success = true,
                Data = mockEmployee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get employee by ID: {Id}", id);
            return StatusCode(500, new ApiResponse<Employee>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An internal error occurred"
                }
            });
        }
    }
}
