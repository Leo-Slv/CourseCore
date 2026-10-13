namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
