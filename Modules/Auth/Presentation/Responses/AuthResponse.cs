namespace CourseCore.Api.Modules.Auth.Presentation.Responses;

public class AuthResponse
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public AuthTokenResponse Token { get; init; } = new();
}
