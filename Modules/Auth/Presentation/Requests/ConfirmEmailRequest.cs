namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class ConfirmEmailRequest
{
    public string Token { get; init; } = string.Empty;
}
