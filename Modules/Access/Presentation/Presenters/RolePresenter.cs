using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;

namespace CourseCore.Api.Modules.Access.Presentation.Presenters;

public static class RolePresenter
{
    public static RoleResponse ToResponse(RoleOutput output)
    {
        return new RoleResponse
        {
            Id = output.Id,
            Name = output.Name
        };
    }

    public static RoleDetailResponse ToResponse(RoleDetailOutput output)
    {
        return new RoleDetailResponse
        {
            Id = output.Id,
            Name = output.Name,
            Description = output.Description,
            Active = output.Active,
            IsSystem = output.IsSystem,
            MemberCount = output.MemberCount,
            PermissionKeys = output.PermissionKeys
        };
    }

    public static PermissionResponse ToResponse(PermissionOutput output)
    {
        return new PermissionResponse
        {
            Id = output.Id,
            Key = output.Key,
            Name = output.Name,
            Description = output.Description
        };
    }

    public static CreateRoleInput ToInput(CreateRoleRequest request)
    {
        return new CreateRoleInput
        {
            Name = request.Name,
            Description = request.Description
        };
    }

    public static UpdateRoleInput ToInput(Guid roleId, UpdateRoleRequest request)
    {
        return new UpdateRoleInput
        {
            RoleId = roleId,
            Name = request.Name,
            Description = request.Description,
            Active = request.Active
        };
    }

    public static SetRolePermissionsInput ToInput(Guid roleId, SetRolePermissionsRequest request)
    {
        return new SetRolePermissionsInput
        {
            RoleId = roleId,
            PermissionKeys = request.PermissionKeys
        };
    }
}
