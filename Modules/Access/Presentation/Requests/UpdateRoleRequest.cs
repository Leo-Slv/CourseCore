namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class UpdateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; } = true;
}
