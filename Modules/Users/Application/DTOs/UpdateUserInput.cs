namespace CourseCore.Api.Modules.Users.Application.DTOs;

public class UpdateUserInput
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }
}
