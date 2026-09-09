namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
