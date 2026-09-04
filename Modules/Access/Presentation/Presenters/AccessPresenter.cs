using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Enums;
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

    public static CheckCourseAccessInput ToInput(Guid userId, CheckCourseAccessRequest request)
    {
        return new CheckCourseAccessInput
        {
            UserId = userId,
            CourseId = request.CourseId
        };
    }

    public static CheckCourseAccessInput ToInput(Guid userId, Guid courseId)
    {
        return new CheckCourseAccessInput
        {
            UserId = userId,
            CourseId = courseId
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

    public static RequestCourseAccessInput ToInput(Guid userId, RequestCourseAccessRequest request)
    {
        return new RequestCourseAccessInput
        {
            UserId = userId,
            CourseId = request.CourseId
        };
    }

    public static ListAccessRequestsInput ToInput(ListAccessRequestsRequest request)
    {
        return new ListAccessRequestsInput { Status = ParseStatus(request.Status) };
    }

    public static ListMyAccessRequestsInput ToListMyAccessRequestsInput(Guid userId)
    {
        return new ListMyAccessRequestsInput { UserId = userId };
    }

    public static ApproveAccessRequestInput ToApproveInput(Guid accessRequestId, Guid decidedByUserId)
    {
        return new ApproveAccessRequestInput
        {
            AccessRequestId = accessRequestId,
            DecidedByUserId = decidedByUserId
        };
    }

    public static RejectAccessRequestInput ToRejectInput(Guid accessRequestId, Guid decidedByUserId)
    {
        return new RejectAccessRequestInput
        {
            AccessRequestId = accessRequestId,
            DecidedByUserId = decidedByUserId
        };
    }

    public static AccessRequestResponse ToResponse(AccessRequestOutput output)
    {
        return new AccessRequestResponse
        {
            Id = output.Id,
            UserId = output.UserId,
            CourseId = output.CourseId,
            Status = output.Status,
            DecidedAt = output.DecidedAt,
            DecidedByUserId = output.DecidedByUserId,
            CreatedAt = output.CreatedAt
        };
    }

    private static AccessRequestStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (Enum.TryParse<AccessRequestStatus>(status, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException("Status is invalid.", nameof(status));
    }
}
