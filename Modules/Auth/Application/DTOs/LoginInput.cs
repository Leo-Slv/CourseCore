namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class LoginInput
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
