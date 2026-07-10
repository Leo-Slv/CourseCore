namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
