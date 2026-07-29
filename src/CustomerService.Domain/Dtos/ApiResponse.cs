namespace CustomerService.Domain.Dtos;

/// <summary>
/// Standard successful API response envelope.
/// </summary>
public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    string TraceId,
    DateTime TimestampUtc)
{
    public static ApiResponse<T> Ok(T? data, string message, string traceId) =>
        new(true, message, data, traceId, DateTime.UtcNow);
}

/// <summary>
/// Standard API error response. Detailed exception information is never exposed.
/// </summary>
public sealed record ApiErrorResponse(
    bool Success,
    string Code,
    string Message,
    string TraceId,
    DateTime TimestampUtc,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ApiErrorResponse Create(
        string code,
        string message,
        string traceId,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(false, code, message, traceId, DateTime.UtcNow, errors);
}
