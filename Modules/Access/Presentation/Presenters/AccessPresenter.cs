using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;

namespace CourseCore.Api.Modules.Access.Presentation.Presenters;

public static class AccessPresenter
{
    public static GrantUserAreaAccessInput ToInput(GrantUserAreaAccessRequest request)
    {
        return new GrantUserAreaAccessInput
        {
            UserId = request.UserId,
            AreaId = request.AreaId,
            CanView = request.CanView,
            CanManage = request.CanManage,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt
        };
    }

    public static GrantRoleAreaAccessInput ToInput(GrantRoleAreaAccessRequest request)
    {
        return new GrantRoleAreaAccessInput
        {
            RoleId = request.RoleId,
            AreaId = request.AreaId,
            CanView = request.CanView,
            CanManage = request.CanManage
        };
    }

    public static CheckCourseAccessInput ToInput(CheckCourseAccessRequest request)
    {
        return new CheckCourseAccessInput
        {
            UserId = request.UserId,
            CourseId = request.CourseId
        };
    }

    public static AreaAccessResponse ToResponse(AreaAccessOutput output)
    {
        return new AreaAccessResponse
        {
            AreaId = output.AreaId,
            CanView = output.CanView,
            CanManage = output.CanManage
        };
    }

    public static CourseAccessResponse ToResponse(CourseAccessOutput output)
    {
        return new CourseAccessResponse
        {
            UserId = output.UserId,
            CourseId = output.CourseId,
            CanAccess = output.CanAccess,
            Reason = output.Reason
        };
    }
}
