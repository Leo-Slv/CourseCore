namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class UpdateOwnProfileInput
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? AvatarUrl { get; init; }
}
