namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class SetRolePermissionsRequest
{
    public IReadOnlyCollection<string> PermissionKeys { get; init; } = [];
}
