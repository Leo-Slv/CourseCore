namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class AccessRequestResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? DecidedAt { get; init; }

    public Guid? DecidedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }
}
