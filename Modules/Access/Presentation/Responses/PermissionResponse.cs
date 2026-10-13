namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class PermissionResponse
{
    public Guid Id { get; init; }

    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
