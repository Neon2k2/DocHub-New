using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Common;

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResult(string errorMessage, string? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError
            {
                Message = errorMessage,
                Details = details
            }
        };
    }
}

public class ApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public class PaginatedResponse<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; } = new List<T>();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;
}