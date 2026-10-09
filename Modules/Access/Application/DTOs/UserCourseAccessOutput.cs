using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class UserCourseAccessOutput
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public string Source { get; init; } = string.Empty;

    public DateTime GrantedAt { get; init; }

    public static UserCourseAccessOutput FromUserCourseAccess(UserCourseAccess access)
    {
        return new UserCourseAccessOutput
        {
            UserId = access.UserId,
            CourseId = access.CourseId,
            Source = access.Source.ToString(),
            GrantedAt = access.CreatedAt
        };
    }
}
