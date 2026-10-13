namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class LoginLockoutOptions
{
    public const string SectionName = "Auth:LoginLockout";

    /// <summary>Failed attempts inside the window that lock the account.</summary>
    public int MaxFailedAttempts { get; init; } = 5;

    /// <summary>Failures older than this are forgotten.</summary>
    public int FailureWindowMinutes { get; init; } = 15;

    /// <summary>How long the account refuses logins once locked.</summary>
    public int LockoutMinutes { get; init; } = 15;
}
