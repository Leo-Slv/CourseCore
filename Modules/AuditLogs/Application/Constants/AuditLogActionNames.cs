namespace CourseCore.Api.Modules.AuditLogs.Application.Constants;

public static class AuditLogActionNames
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string RefreshTokenRotated = "RefreshTokenRotated";
    public const string RefreshTokenRejected = "RefreshTokenRejected";
    public const string RefreshTokenReplayDetected = "RefreshTokenReplayDetected";
    public const string LogoutSucceeded = "LogoutSucceeded";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserTokenVersionIncremented = "UserTokenVersionIncremented";
    public const string UserSessionsRevoked = "UserSessionsRevoked";
    public const string UserAreaAccessGranted = "UserAreaAccessGranted";
    public const string RoleAreaAccessGranted = "RoleAreaAccessGranted";
    public const string CourseCreated = "CourseCreated";
    public const string CourseUpdated = "CourseUpdated";
    public const string CoursePublished = "CoursePublished";
    public const string VideoCreated = "VideoCreated";
    public const string VideoMarkedReady = "VideoMarkedReady";
}
