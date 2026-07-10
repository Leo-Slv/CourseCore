namespace CourseCore.Api.Shared.Presentation.Responses;

public sealed class ApiErrorResponse
{
    public int StatusCode { get; init; }

    public string Error { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public IReadOnlyCollection<string> Details { get; init; } = Array.Empty<string>();
}
