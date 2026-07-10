namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class AuthToken
{
    public string AccessToken { get; init; } = string.Empty;

    public string? RefreshToken { get; init; }

    public DateTime ExpiresAt { get; init; }
}
