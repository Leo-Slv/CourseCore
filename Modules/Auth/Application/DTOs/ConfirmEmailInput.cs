namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public class ConfirmEmailInput
{
    public Guid UserId { get; init; }

    public string Token { get; init; } = string.Empty;
}
