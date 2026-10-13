using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class RoleDetailOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; }

    public bool IsSystem { get; init; }

    public int MemberCount { get; init; }

    public IReadOnlyCollection<string> PermissionKeys { get; init; } = [];

    public static RoleDetailOutput FromRole(
        Role role,
        bool isSystem,
        int memberCount,
        IReadOnlyCollection<string> permissionKeys)
    {
        return new RoleDetailOutput
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Active = role.Active,
            IsSystem = isSystem,
            MemberCount = memberCount,
            PermissionKeys = permissionKeys
        };
    }
}
