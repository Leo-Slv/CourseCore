namespace CourseCore.Api.Modules.Visitors.Presentation.Requests;

public class RegisterVisitorRequest
{
    public string Name { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Address { get; init; }

    public string CaptchaToken { get; init; } = string.Empty;
}
