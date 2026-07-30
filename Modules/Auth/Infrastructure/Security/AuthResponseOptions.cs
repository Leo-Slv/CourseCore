namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class AuthResponseOptions
{
    public bool ExposeRefreshTokenInBody { get; init; }

    public bool AllowRefreshTokenInBodyFallback { get; init; }
}
