namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class RequestPasswordResetRequest
{
    public string Email { get; init; } = string.Empty;

    public string CaptchaToken { get; init; } = string.Empty;
}
