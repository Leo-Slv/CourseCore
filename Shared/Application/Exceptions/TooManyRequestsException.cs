namespace CourseCore.Api.Shared.Application.Exceptions;

/// <summary>
/// The caller must wait before trying again (mapped to 429 with a Retry-After header).
/// </summary>
public sealed class TooManyRequestsException : ApplicationExceptionBase
{
    public TooManyRequestsException(string message, TimeSpan retryAfter)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan RetryAfter { get; }
}
