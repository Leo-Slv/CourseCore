namespace CourseCore.Api.Modules.Users.Presentation.Responses;

public class UserResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }

    public DateTime? EmailVerifiedAt { get; init; }

    public IReadOnlyCollection<string> RoleNames { get; init; } = Array.Empty<string>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
