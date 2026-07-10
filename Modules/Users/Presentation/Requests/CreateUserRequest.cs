namespace CourseCore.Api.Modules.Users.Presentation.Requests;

public class CreateUserRequest
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
