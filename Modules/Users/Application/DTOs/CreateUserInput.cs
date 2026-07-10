namespace CourseCore.Api.Modules.Users.Application.DTOs;

public class CreateUserInput
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
