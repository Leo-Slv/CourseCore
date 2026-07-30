namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
