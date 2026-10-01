namespace CourseCore.Api.Modules.Visitors.Application.DTOs;

public sealed class RegisterVisitorOutput
{
    public Guid Id { get; init; }

    public DateTime SubmittedAt { get; init; }
}
