using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class PermissionOutput
{
    public Guid Id { get; init; }

    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public static PermissionOutput FromPermission(Permission permission)
    {
        return new PermissionOutput
        {
            Id = permission.Id,
            Key = permission.Key,
            Name = permission.Name,
            Description = permission.Description
        };
    }
}
