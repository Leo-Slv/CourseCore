namespace CourseCore.Api.Shared.Presentation.RateLimiting;

public static class RateLimitPolicyNames
{
    public const string AuthLogin = "AuthLogin";
    public const string AuthRefresh = "AuthRefresh";
    public const string AuthLogout = "AuthLogout";
    public const string AuthRegister = "AuthRegister";
    public const string AuthResendConfirmation = "AuthResendConfirmation";
}
