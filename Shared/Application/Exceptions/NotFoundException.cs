namespace CourseCore.Api.Shared.Application.Exceptions;

public sealed class NotFoundException : ApplicationExceptionBase
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
