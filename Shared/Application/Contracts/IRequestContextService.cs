namespace CourseCore.Api.Shared.Application.Contracts;

public interface IRequestContextService
{
    string? CorrelationId { get; }
}
