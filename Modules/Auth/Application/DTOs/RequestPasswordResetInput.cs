namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class RequestPasswordResetInput
{
    public string Email { get; init; } = string.Empty;

    public string CaptchaToken { get; init; } = string.Empty;
}
