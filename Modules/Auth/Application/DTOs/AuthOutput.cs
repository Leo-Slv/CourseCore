namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class AuthOutput
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public AuthToken Token { get; init; } = new();
}
