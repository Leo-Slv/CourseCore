using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class UserCourseAccessMapper
{
    public static UserCourseAccess ToDomain(UserCourseAccessPersistenceModel model)
    {
        return UserCourseAccess.Restore(
            model.Id,
            model.UserId,
            model.CourseId,
            (CourseAccessSource)model.Source,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static UserCourseAccessPersistenceModel ToPersistence(UserCourseAccess access)
    {
        return new UserCourseAccessPersistenceModel
        {
            Id = access.Id,
            UserId = access.UserId,
            CourseId = access.CourseId,
            Source = (int)access.Source,
            CreatedAt = access.CreatedAt,
            UpdatedAt = access.UpdatedAt
        };
    }
}
