namespace CourseCore.Api.Shared.Presentation.RateLimiting;

public static class RateLimitPolicyNames
{
    public const string AuthLogin = "AuthLogin";
    public const string AuthRefresh = "AuthRefresh";
    public const string AuthLogout = "AuthLogout";
    public const string AuthRegister = "AuthRegister";
    public const string AuthResendConfirmation = "AuthResendConfirmation";
    public const string AuthForgotPassword = "AuthForgotPassword";
    public const string AuthResetPassword = "AuthResetPassword";
    public const string AuthChangePassword = "AuthChangePassword";
    public const string VisitorRegistration = "VisitorRegistration";
    public const string Checkout = "Checkout";
    public const string Upload = "Upload";
    public const string UserContent = "UserContent";
}
