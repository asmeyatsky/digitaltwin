using System;

namespace DigitalTwin.Core.DTOs
{
    /// <summary>
    /// Standard API response envelope for all non-auth endpoints.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string? message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> Fail(string error) => new()
        {
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Non-generic variant for void / message-only responses.
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse Ok(string? message = null) => new()
        {
            Success = true,
            Message = message
        };

        public static ApiResponse Fail(string error) => new()
        {
            Success = false,
            Error = error
        };
    }
}
