namespace FileIt.Module.Complex.App.Errors;

/// <summary>
/// RFC 7807 Problem Details response body. Used for every non-success
/// response so clients see a consistent error shape.
/// </summary>
/// <remarks>
/// We include both the standard fields (type/title/status/detail/instance)
/// and a few extension fields the FileIt operator dashboards need:
/// correlationId for cross-module tracing, traceId for App Insights, and
/// errors for field-level validation feedback.
/// </remarks>
public sealed class ProblemDetails
{
    public string Type { get; set; } = "about:blank";
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }

    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }

    public IDictionary<string, string[]>? Errors { get; set; }
}

public static class ProblemDetailsFactory
{
    public const string ProblemContentType = "application/problem+json";

    public static ProblemDetails BadRequest(string detail, string? instance = null, IDictionary<string, string[]>? errors = null)
        => new()
        {
            Type = "https://fileit/problems/bad-request",
            Title = "Bad Request",
            Status = 400,
            Detail = detail,
            Instance = instance,
            Errors = errors,
        };

    public static ProblemDetails NotFound(string detail, string? instance = null)
        => new()
        {
            Type = "https://fileit/problems/not-found",
            Title = "Not Found",
            Status = 404,
            Detail = detail,
            Instance = instance,
        };

    public static ProblemDetails Conflict(string detail, string? instance = null)
        => new()
        {
            Type = "https://fileit/problems/conflict",
            Title = "Conflict",
            Status = 409,
            Detail = detail,
            Instance = instance,
        };

    public static ProblemDetails PayloadTooLarge(string detail, string? instance = null)
        => new()
        {
            Type = "https://fileit/problems/payload-too-large",
            Title = "Payload Too Large",
            Status = 413,
            Detail = detail,
            Instance = instance,
        };

    public static ProblemDetails ServiceUnavailable(string detail, int retryAfterSeconds, string? instance = null)
        => new()
        {
            Type = "https://fileit/problems/service-unavailable",
            Title = "Service Unavailable",
            Status = 503,
            Detail = detail,
            Instance = instance,
        };

    public static ProblemDetails InternalError(string detail, string? instance = null)
        => new()
        {
            Type = "https://fileit/problems/internal-error",
            Title = "Internal Server Error",
            Status = 500,
            Detail = detail,
            Instance = instance,
        };
}
