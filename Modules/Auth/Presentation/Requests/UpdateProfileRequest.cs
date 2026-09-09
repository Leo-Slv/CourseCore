namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class UpdateProfileRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? AvatarUrl { get; init; }
}
