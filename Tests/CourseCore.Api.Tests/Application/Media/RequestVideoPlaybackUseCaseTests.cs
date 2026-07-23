using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.Application.Media;

public class RequestVideoPlaybackUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserHasAccess_ShouldUseCourseFoundByLessonId()
    {
        var fixture = CreateFixture(grantAccess: true);

        await fixture.UseCase.ExecuteAsync(new RequestVideoPlaybackInput
        {
            UserId = fixture.UserId,
            VideoId = fixture.Video.Id
        });

        Assert.Equal(1, fixture.Courses.FindByLessonIdCalls);
        Assert.Equal(0, fixture.Courses.ListCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseForLessonIsNotFound_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(grantAccess: true, addCourse: false);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new RequestVideoPlaybackInput
        {
            UserId = fixture.UserId,
            VideoId = fixture.Video.Id
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(grantAccess: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UseCase.ExecuteAsync(new RequestVideoPlaybackInput
        {
            UserId = fixture.UserId,
            VideoId = fixture.Video.Id
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasAccess_ShouldReturnPlaybackUrl()
    {
        var fixture = CreateFixture(grantAccess: true);

        var output = await fixture.UseCase.ExecuteAsync(new RequestVideoPlaybackInput
        {
            UserId = fixture.UserId,
            VideoId = fixture.Video.Id
        });

        Assert.Equal(fixture.Storage.PlaybackUrl, output.PlaybackUrl);
        Assert.Equal(fixture.Video.Id, output.VideoId);
        Assert.Equal(fixture.Lesson.Id, output.LessonId);
    }

    private static RequestVideoPlaybackFixture CreateFixture(bool grantAccess, bool addCourse = true)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var lessons = new FakeLessonRepository();
        var videos = new FakeVideoRepository();
        var storage = new FakeVideoStorageService();
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var (course, lesson) = CreatePublishedCourseWithLesson(area.Id);
        var video = Video.Create(
            lesson.Id,
            "Video",
            "Description",
            VideoStorageProvider.Local,
            "video-key",
            durationSeconds: 120,
            sizeBytes: 1024);
        video.MarkAsReady("stored-playback-url");

        users.Add(user);
        areas.Areas.Add(area);
        lessons.Lessons.Add(lesson);
        videos.Videos.Add(video);

        if (addCourse)
        {
            courses.Courses.Add(course);
        }

        if (grantAccess)
        {
            areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: false));
        }

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var useCase = new RequestVideoPlaybackUseCase(videos, lessons, courses, courseAccessService, storage);

        return new RequestVideoPlaybackFixture(useCase, courses, storage, user.Id, video, lesson);
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

    private sealed record RequestVideoPlaybackFixture(
        RequestVideoPlaybackUseCase UseCase,
        FakeCourseRepository Courses,
        FakeVideoStorageService Storage,
        Guid UserId,
        Video Video,
        Lesson Lesson);
}
