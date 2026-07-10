namespace CourseCore.Api.Shared.Application.Exceptions;

public sealed class ApplicationValidationException : ApplicationExceptionBase
{
    public ApplicationValidationException(string message)
        : base(message)
    {
    }

    public ApplicationValidationException(
        string message,
        IReadOnlyCollection<string> details)
        : base(message)
    {
        Details = details;
    }

    public IReadOnlyCollection<string> Details { get; } = Array.Empty<string>();
}
