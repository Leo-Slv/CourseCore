namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class ChangeOwnPasswordInput
{
    public Guid UserId { get; init; }

    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
