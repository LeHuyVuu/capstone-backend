namespace capstone_backend.Api.Models;

/// <summary>
/// Standard API response wrapper for all endpoints
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
/// <remarks>
/// Provides consistent response format across the entire API:
/// { "message": "...", "code": 200, "data": {...}, "traceId": "..." }
/// </remarks>
public class ApiResponse<T>
{
    /// <summary>
    /// Human-readable message describing the response
    /// </summary>
    /// <example>User created successfully</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code
    /// </summary>
    /// <example>200</example>
    public int Code { get; set; }

    /// <summary>
    /// The actual response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Trace ID for request tracking and logging correlation
    /// </summary>
    /// <example>0HMVB3QK3QK3Q:00000001</example>
    public string? TraceId { get; set; }

    /// <summary>
    /// Timestamp when the response was generated (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Create a success response
    /// </summary>
    public static ApiResponse<T> Success(T? data, string message = "Success", int code = 200, string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Message = message,
            Code = code,
            Data = data,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    public static ApiResponse<T> Error(string message, int code = 500, string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Message = message,
            Code = code,
            Data = default,
            TraceId = traceId
        };
    }
}
