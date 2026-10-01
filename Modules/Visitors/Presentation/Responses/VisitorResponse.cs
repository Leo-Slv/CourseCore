namespace CourseCore.Api.Modules.Visitors.Presentation.Responses;

public class VisitorResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Address { get; init; }

    public DateTime SubmittedAt { get; init; }
}
