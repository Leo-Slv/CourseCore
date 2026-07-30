namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class RefreshTokenCookieOptions
{
    public string Name { get; init; } = "coursecore_refresh_token";

    public string Path { get; init; } = "/api/auth";

    public string SameSite { get; init; } = "Lax";

    public bool Secure { get; init; }

    public string? Domain { get; init; }

    public int MaxAgeDays { get; init; } = 7;
}
