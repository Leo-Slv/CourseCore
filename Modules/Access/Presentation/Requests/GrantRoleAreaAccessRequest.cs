namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class GrantRoleAreaAccessRequest
{
    public Guid RoleId { get; init; }

    public Guid AreaId { get; init; }

    public bool CanView { get; init; }

    public bool CanManage { get; init; }
}
