using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.TestDoubles;

public static class TestEntityFactory
{
    public static User User(
        Guid? id = null,
        string email = "user@coursecore.local",
        string passwordHash = "hashed:password",
        bool active = true)
    {
        var now = DateTime.UtcNow.AddMinutes(-5);

        return CourseCore.Api.Modules.Users.Domain.Entities.User.Restore(
            id ?? Guid.NewGuid(),
            "Test User",
            Email.Create(email),
            passwordHash,
            active,
            emailVerifiedAt: null,
            now,
            now);
    }

    public static Role Role(Guid? id = null, string name = "Admin")
    {
        var now = DateTime.UtcNow.AddMinutes(-5);

        return CourseCore.Api.Modules.Access.Domain.Entities.Role.Restore(
            id ?? Guid.NewGuid(),
            name,
            "Role",
            active: true,
            now,
            now);
    }

    public static Area Area(Guid? id = null, bool active = true)
    {
        var now = DateTime.UtcNow.AddMinutes(-5);

        return CourseCore.Api.Modules.Access.Domain.Entities.Area.Restore(
            id ?? Guid.NewGuid(),
            "Area",
            Slug.Create($"area-{Guid.NewGuid():N}"),
            "Area",
            active,
            displayOrder: 0,
            now,
            now);
    }

    public static Course PublishedCourse(Guid areaId)
    {
        var course = Course.Create(
            "Course",
            Slug.Create($"course-{Guid.NewGuid():N}"),
            "Course",
            displayOrder: 0);

        course.AttachArea(areaId);
        course.Publish();

        return course;
    }
}
