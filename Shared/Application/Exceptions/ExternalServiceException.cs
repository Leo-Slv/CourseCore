namespace CourseCore.Api.Shared.Application.Exceptions;

public sealed class ExternalServiceException : ApplicationExceptionBase
{
    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
