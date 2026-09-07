namespace CourseCore.Api.Modules.Auth.Presentation.Responses;

public class CurrentUserResponse
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }

    public DateTime? EmailVerifiedAt { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
