namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class TurnstileOptions
{
    public string SecretKey { get; init; } = string.Empty;
}
