namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class ConfirmPasswordResetRequest
{
    public string Token { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
