using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Progress;

public class RegisterLessonProgressUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserHasAccess_ShouldUseCourseFoundByLessonId()
    {
        var fixture = CreateFixture(grantAccess: true);

        await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 60
        });

        Assert.Equal(1, fixture.Courses.FindByLessonIdCalls);
        Assert.Equal(0, fixture.Courses.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseForLessonIsNotFound_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(grantAccess: true, addCourse: false);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 60
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(grantAccess: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 60
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasAccess_ShouldRegisterProgress()
    {
        var fixture = CreateFixture(grantAccess: true);

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 90,
            MarkAsCompleted = true
        });

        Assert.Equal(90, output.WatchedSeconds);
        Assert.True(output.Completed);
        Assert.Single(fixture.Progress.SavedLessonProgresses);
        Assert.Single(fixture.Progress.SavedCourseProgresses);
    }

    private static RegisterLessonProgressFixture CreateFixture(bool grantAccess, bool addCourse = true)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var lessons = new FakeLessonRepository();
        var progress = new FakeProgressRepository();
        var unitOfWork = new FakeUnitOfWork();
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var (course, lesson) = CreatePublishedCourseWithLesson(area.Id);

        users.Add(user);
        areas.Areas.Add(area);
        lessons.Lessons.Add(lesson);

        if (addCourse)
        {
            courses.Courses.Add(course);
        }

        if (grantAccess)
        {
            areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: false));
        }

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var useCase = new RegisterLessonProgressUseCase(
            users,
            lessons,
            courses,
            progress,
            courseAccessService,
            unitOfWork);

        return new RegisterLessonProgressFixture(useCase, courses, progress, user.Id, lesson);
    }

    private static (Course Course, Lesson Lesson) CreatePublishedCourseWithLesson(Guid areaId)
    {
        var course = Course.Create(
            "Course",
            Slug.Create($"course-{Guid.NewGuid():N}"),
            "Description",
            displayOrder: 0);
        var module = CourseModule.Create(course.Id, "Module", "Description", displayOrder: 0);
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", displayOrder: 0);

        module.AddLesson(lesson);
        course.AddModule(module);
        course.AttachArea(areaId);
        course.Publish();

        return (course, lesson);
    }

    private sealed record RegisterLessonProgressFixture(
        RegisterLessonProgressUseCase UseCase,
        FakeCourseRepository Courses,
        FakeProgressRepository Progress,
        Guid UserId,
        Lesson Lesson);
}
