namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class CurrentUserOutput
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }

    public DateTime? EmailVerifiedAt { get; init; }

    public string? Phone { get; init; }

    public string? AvatarUrl { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
