using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class RoleOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public static RoleOutput FromRole(Role role)
    {
        return new RoleOutput
        {
            Id = role.Id,
            Name = role.Name
        };
    }
}
