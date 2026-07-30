using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Application.Options;
using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

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
        fixture.Videos.Videos.Add(CreateReadyVideo(fixture.Lesson.Id, durationSeconds: 100));

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

    [Fact]
    public async Task ExecuteAsync_WhenClientMarksCompletedWithZeroSeconds_ShouldNotCompleteLesson()
    {
        var fixture = CreateFixture(grantAccess: true);
        fixture.Videos.Videos.Add(CreateReadyVideo(fixture.Lesson.Id, durationSeconds: 100));

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 0,
            MarkAsCompleted = true
        });

        Assert.Equal(0, output.WatchedSeconds);
        Assert.False(output.Completed);
        Assert.Null(output.CompletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWatchedSecondsAreBelowThreshold_ShouldNotCompleteLesson()
    {
        var fixture = CreateFixture(grantAccess: true);
        fixture.Videos.Videos.Add(CreateReadyVideo(fixture.Lesson.Id, durationSeconds: 100));

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 89,
            MarkAsCompleted = true
        });

        Assert.False(output.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWatchedSecondsReachThreshold_ShouldCompleteLesson()
    {
        var fixture = CreateFixture(grantAccess: true);
        fixture.Videos.Videos.Add(CreateReadyVideo(fixture.Lesson.Id, durationSeconds: 100));

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 90
        });

        Assert.True(output.Completed);
        Assert.NotNull(output.CompletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWatchedSecondsExceedVideoDuration_ShouldClampWatchedSeconds()
    {
        var fixture = CreateFixture(grantAccess: true);
        fixture.Videos.Videos.Add(CreateReadyVideo(fixture.Lesson.Id, durationSeconds: 120));

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 300
        });

        Assert.Equal(120, output.WatchedSeconds);
        Assert.True(output.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWatchedSecondsDecrease_ShouldKeepExistingProgress()
    {
        var fixture = CreateFixture(grantAccess: true);
        fixture.Videos.Videos.Add(CreateReadyVideo(fixture.Lesson.Id, durationSeconds: 600));
        var existingProgress = UserLessonProgress.Create(fixture.UserId, fixture.Lesson.Id);
        existingProgress.RegisterWatch(300);
        await fixture.Progress.SaveLessonProgressAsync(existingProgress);

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 10
        });

        Assert.Equal(300, output.WatchedSeconds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOnlyOneOfTwoLessonsIsCompleted_ShouldRecalculateCourseAsHalfComplete()
    {
        var fixture = CreateFixture(grantAccess: true, lessonCount: 2);
        var firstLesson = fixture.Course.Modules.Single().Lessons.ElementAt(0);

        fixture.Videos.Videos.Add(CreateReadyVideo(firstLesson.Id, durationSeconds: 100));

        await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = firstLesson.Id,
            WatchedSeconds = 90,
            MarkAsCompleted = true
        });

        var courseProgress = Assert.Single(fixture.Progress.SavedCourseProgresses);
        Assert.Equal(50, courseProgress.ProgressPercent);
        Assert.Null(courseProgress.CompletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllLessonsAreCompletedByServer_ShouldCompleteCourse()
    {
        var fixture = CreateFixture(grantAccess: true, lessonCount: 2);
        var lessons = fixture.Course.Modules.Single().Lessons.ToArray();

        fixture.Videos.Videos.Add(CreateReadyVideo(lessons[0].Id, durationSeconds: 100));
        fixture.Videos.Videos.Add(CreateReadyVideo(lessons[1].Id, durationSeconds: 100));

        await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = lessons[0].Id,
            WatchedSeconds = 90
        });
        await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = lessons[1].Id,
            WatchedSeconds = 90
        });

        var courseProgress = fixture.Progress.SavedCourseProgresses.Last();
        Assert.Equal(100, courseProgress.ProgressPercent);
        Assert.NotNull(courseProgress.CompletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonHasNoVideo_ShouldNotCompleteLesson()
    {
        var fixture = CreateFixture(grantAccess: true);

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 10_000,
            MarkAsCompleted = true
        });

        Assert.False(output.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVideoIsNotReady_ShouldNotCompleteLesson()
    {
        var fixture = CreateFixture(grantAccess: true);
        fixture.Videos.Videos.Add(CreateProcessingVideo(fixture.Lesson.Id, durationSeconds: 100));

        var output = await fixture.UseCase.ExecuteAsync(new RegisterLessonProgressInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            WatchedSeconds = 100,
            MarkAsCompleted = true
        });

        Assert.Equal(100, output.WatchedSeconds);
        Assert.False(output.Completed);
    }

    private static RegisterLessonProgressFixture CreateFixture(
        bool grantAccess,
        bool addCourse = true,
        int lessonCount = 1)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var lessons = new FakeLessonRepository();
        var videos = new FakeVideoRepository();
        var progress = new FakeProgressRepository();
        var unitOfWork = new FakeUnitOfWork();
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var (course, lesson) = CreatePublishedCourseWithLesson(area.Id, lessonCount);

        users.Add(user);
        areas.Areas.Add(area);
        foreach (var moduleLesson in course.Modules.SelectMany(module => module.Lessons))
        {
            lessons.Lessons.Add(moduleLesson);
        }

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
            videos,
            progress,
            courseAccessService,
            unitOfWork,
            Options.Create(new ProgressOptions()));

        return new RegisterLessonProgressFixture(useCase, courses, videos, progress, user.Id, course, lesson);
    }

    private static (Course Course, Lesson Lesson) CreatePublishedCourseWithLesson(Guid areaId, int lessonCount)
    {
        var course = Course.Create(
            "Course",
            Slug.Create($"course-{Guid.NewGuid():N}"),
            "Description",
            displayOrder: 0);
        var module = CourseModule.Create(course.Id, "Module", "Description", displayOrder: 0);
        Lesson? firstLesson = null;

        for (var index = 0; index < lessonCount; index++)
        {
            var lesson = Lesson.Create(module.Id, $"Lesson {index}", "Description", index);
            firstLesson ??= lesson;
            module.AddLesson(lesson);
        }

        course.AddModule(module);
        course.AttachArea(areaId);
        course.Publish();

        return (course, firstLesson ?? throw new InvalidOperationException("At least one lesson is required."));
    }

    private static Video CreateReadyVideo(Guid lessonId, int durationSeconds)
    {
        var video = CreateProcessingVideo(lessonId, durationSeconds);
        video.MarkAsReady();

        return video;
    }

    private static Video CreateProcessingVideo(Guid lessonId, int durationSeconds)
    {
        return Video.Create(
            lessonId,
            "Video",
            "Description",
            VideoStorageProvider.Local,
            $"videos/{Guid.NewGuid():N}.mp4",
            durationSeconds,
            sizeBytes: 1024);
    }

    private sealed record RegisterLessonProgressFixture(
        RegisterLessonProgressUseCase UseCase,
        FakeCourseRepository Courses,
        FakeVideoRepository Videos,
        FakeProgressRepository Progress,
        Guid UserId,
        Course Course,
        Lesson Lesson);
}
