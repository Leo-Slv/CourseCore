using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class GetCourseDetailsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCourseDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(addCourse: false, grantAccessToInputUser: true);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new GetCourseDetailsInput
        {
            UserId = fixture.InputUserId,
            CourseId = fixture.CourseId
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(grantAccessToInputUser: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UseCase.ExecuteAsync(new GetCourseDetailsInput
        {
            UserId = fixture.InputUserId,
            CourseId = fixture.CourseId
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasAccess_ShouldReturnCourseDetails()
    {
        var fixture = CreateFixture(grantAccessToInputUser: true);

        var output = await fixture.UseCase.ExecuteAsync(new GetCourseDetailsInput
        {
            UserId = fixture.InputUserId,
            CourseId = fixture.CourseId
        });

        Assert.Equal(fixture.CourseId, output.Id);
        Assert.Equal("Course", output.Title);
        Assert.Equal(fixture.AreaId, Assert.Single(output.AreaIds));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldValidateAccessUsingInputUserId()
    {
        var fixture = CreateFixture(grantAccessToInputUser: false, grantAccessToOtherUser: true);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UseCase.ExecuteAsync(new GetCourseDetailsInput
        {
            UserId = fixture.InputUserId,
            CourseId = fixture.CourseId
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonHasVideo_ShouldIncludeVideoIdAndDuration()
    {
        var fixture = CreateFixture(grantAccessToInputUser: true);
        var module = CourseModule.Create(fixture.CourseId, "Module", "Module", 0);
        var lesson = Lesson.Create(module.Id, "Lesson", "Lesson", 0);
        module.AddLesson(lesson);
        fixture.Course.AddModule(module);
        var video = Video.Create(
            lesson.Id, "Video", "Video", VideoStorageProvider.Local, "videos/v.mp4", durationSeconds: 240, sizeBytes: 10);
        fixture.Videos.Videos.Add(video);

        var output = await fixture.UseCase.ExecuteAsync(new GetCourseDetailsInput
        {
            UserId = fixture.InputUserId,
            CourseId = fixture.CourseId
        });

        var lessonOutput = Assert.Single(Assert.Single(output.Modules).Lessons);
        Assert.Equal(video.Id, lessonOutput.VideoId);
        Assert.Equal(240, lessonOutput.DurationSeconds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonHasNoVideo_ShouldReturnNullVideoFields()
    {
        var fixture = CreateFixture(grantAccessToInputUser: true);
        var module = CourseModule.Create(fixture.CourseId, "Module", "Module", 0);
        var lesson = Lesson.Create(module.Id, "Lesson", "Lesson", 0);
        module.AddLesson(lesson);
        fixture.Course.AddModule(module);

        var output = await fixture.UseCase.ExecuteAsync(new GetCourseDetailsInput
        {
            UserId = fixture.InputUserId,
            CourseId = fixture.CourseId
        });

        var lessonOutput = Assert.Single(Assert.Single(output.Modules).Lessons);
        Assert.Null(lessonOutput.VideoId);
        Assert.Null(lessonOutput.DurationSeconds);
    }

    private static GetCourseDetailsFixture CreateFixture(
        bool addCourse = true,
        bool grantAccessToInputUser = false,
        bool grantAccessToOtherUser = false)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var inputUser = TestEntityFactory.User(email: $"input-{Guid.NewGuid():N}@coursecore.local");
        var otherUser = TestEntityFactory.User(email: $"other-{Guid.NewGuid():N}@coursecore.local");
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id);

        users.Add(inputUser);
        users.Add(otherUser);
        areas.Areas.Add(area);

        if (addCourse)
        {
            courses.Courses.Add(course);
        }

        if (grantAccessToInputUser)
        {
            areas.UserAreaAccesses.Add(UserAreaAccess.Create(inputUser.Id, area.Id, canView: true, canManage: false));
        }

        if (grantAccessToOtherUser)
        {
            areas.UserAreaAccesses.Add(UserAreaAccess.Create(otherUser.Id, area.Id, canView: true, canManage: false));
        }

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var videos = new FakeVideoRepository();
        var useCase = new GetCourseDetailsUseCase(courses, courseAccessService, videos);

        return new GetCourseDetailsFixture(
            useCase,
            inputUser.Id,
            otherUser.Id,
            course.Id,
            area.Id,
            course,
            videos);
    }

    private sealed record GetCourseDetailsFixture(
        GetCourseDetailsUseCase UseCase,
        Guid InputUserId,
        Guid OtherUserId,
        Guid CourseId,
        Guid AreaId,
        Course Course,
        FakeVideoRepository Videos);
}
