namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class RegisterRequest
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string CaptchaToken { get; init; } = string.Empty;
}
