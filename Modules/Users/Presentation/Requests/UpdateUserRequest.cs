namespace CourseCore.Api.Modules.Users.Presentation.Requests;

public class UpdateUserRequest
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }
}
