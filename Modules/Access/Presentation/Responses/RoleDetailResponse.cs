namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class RoleDetailResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; }

    public bool IsSystem { get; init; }

    public int MemberCount { get; init; }

    public IReadOnlyCollection<string> PermissionKeys { get; init; } = [];
}
