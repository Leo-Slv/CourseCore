namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class SetRolePermissionsInput
{
    public Guid RoleId { get; init; }

    public IReadOnlyCollection<string> PermissionKeys { get; init; } = [];
}
