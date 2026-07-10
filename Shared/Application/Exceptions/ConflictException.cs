namespace CourseCore.Api.Shared.Application.Exceptions;

public sealed class ConflictException : ApplicationExceptionBase
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
