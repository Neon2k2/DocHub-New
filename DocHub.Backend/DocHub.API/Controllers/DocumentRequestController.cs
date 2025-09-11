using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DocHub.API.DTOs;
using DocHub.API.Models;

namespace DocHub.API.Controllers;

[ApiController]
[Route("api/v1/documentrequests")]
// [Authorize] // Temporarily disabled for testing
public class DocumentRequestController : ControllerBase
{
    private readonly ILogger<DocumentRequestController> _logger;

    public DocumentRequestController(ILogger<DocumentRequestController> _logger)
    {
        this._logger = _logger;
    }

    /// <summary>
    /// Get document requests with pagination
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse<PagedResult<DocumentRequest>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 1000,
        [FromQuery] string? documentType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? approverId = null)
    {
        try
        {
            // For now, return empty data until we implement the actual service
            var mockRequests = new List<DocumentRequest>();

            var paginatedResponse = new PagedResult<DocumentRequest>
            {
                Items = mockRequests,
                TotalCount = 0,
                PageNumber = page,
                PageSize = limit
            };

            return Ok(new ApiResponse<PagedResult<DocumentRequest>>
            {
                Success = true,
                Data = paginatedResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document requests");
            return StatusCode(500, new ApiResponse<PagedResult<DocumentRequest>>
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
    /// Get document request by ID
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<ApiResponse<DocumentRequest>> GetById(Guid id)
    {
        try
        {
            return NotFound(new ApiResponse<DocumentRequest>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = "Document request not found"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document request by ID: {Id}", id);
            return StatusCode(500, new ApiResponse<DocumentRequest>
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
