namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class UpdateRoleInput
{
    public Guid RoleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; }
}
