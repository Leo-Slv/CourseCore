namespace CourseCore.Api.Modules.Visitors.Application.DTOs;

public sealed class RegisterVisitorInput
{
    public string Name { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Address { get; init; }

    public string CaptchaToken { get; init; } = string.Empty;
}
