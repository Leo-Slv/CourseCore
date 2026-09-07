using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class LessonVideoUseCaseTests
{
    [Fact]
    public async Task GetLessonVideoUseCase_WhenVideoExists_ShouldReturnVideo()
    {
        var videos = new FakeVideoRepository();
        var lessonId = Guid.NewGuid();
        var video = Video.Create(lessonId, "Video", "Description", VideoStorageProvider.YouTube, "dQw4w9WgXcQ", 100, 0);
        videos.Videos.Add(video);
        var useCase = new GetLessonVideoUseCase(videos);

        var output = await useCase.ExecuteAsync(lessonId);

        Assert.Equal(video.Id, output.Id);
        Assert.Equal("YouTube", output.StorageProvider);
    }

    [Fact]
    public async Task GetLessonVideoUseCase_WhenNoVideoExists_ShouldThrowNotFoundException()
    {
        var useCase = new GetLessonVideoUseCase(new FakeVideoRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReplaceLessonVideoUseCase_WhenNoVideoExists_ShouldCreateVideo()
    {
        var videos = new FakeVideoRepository();
        var lessons = new FakeLessonRepository();
        var lesson = CreateLesson();
        lessons.Lessons.Add(lesson);
        var useCase = new ReplaceLessonVideoUseCase(videos, lessons, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = lesson.Id,
            Title = "Video",
            Description = "Description",
            StorageProvider = "YouTube",
            StorageKey = "dQw4w9WgXcQ",
            DurationSeconds = 120,
            SizeBytes = 0
        });

        Assert.Single(videos.Videos);
        Assert.Equal(lesson.Id, output.LessonId);
    }

    [Fact]
    public async Task ReplaceLessonVideoUseCase_WhenVideoExists_ShouldUpdateInPlace()
    {
        var videos = new FakeVideoRepository();
        var lessons = new FakeLessonRepository();
        var lesson = CreateLesson();
        lessons.Lessons.Add(lesson);
        var existingVideo = Video.Create(lesson.Id, "Old", "Old description", VideoStorageProvider.Local, "old-key", 50, 10);
        existingVideo.MarkAsReady();
        videos.Videos.Add(existingVideo);
        var useCase = new ReplaceLessonVideoUseCase(videos, lessons, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = lesson.Id,
            Title = "New Title",
            Description = "New description",
            StorageProvider = "YouTube",
            StorageKey = "newVideoId1",
            DurationSeconds = 200,
            SizeBytes = 0
        });

        Assert.Single(videos.Videos);
        Assert.Equal(existingVideo.Id, output.Id);
        Assert.Equal("New Title", output.Title);
        Assert.Equal("YouTube", output.StorageProvider);
        Assert.Equal("Processing", output.Status);
    }

    [Fact]
    public async Task RemoveLessonVideoUseCase_WhenVideoExists_ShouldRemoveVideo()
    {
        var videos = new FakeVideoRepository();
        var lessonId = Guid.NewGuid();
        var video = Video.Create(lessonId, "Video", "Description", VideoStorageProvider.YouTube, "dQw4w9WgXcQ", 100, 0);
        videos.Videos.Add(video);
        var useCase = new RemoveLessonVideoUseCase(videos, new FakeUnitOfWork(), new FakeAuditLogService());

        await useCase.ExecuteAsync(lessonId);

        Assert.Empty(videos.Videos);
    }

    [Fact]
    public async Task RemoveLessonVideoUseCase_WhenNoVideoExists_ShouldThrowNotFoundException()
    {
        var useCase = new RemoveLessonVideoUseCase(new FakeVideoRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    private static Lesson CreateLesson()
    {
        var course = Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", 0);
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);

        return Lesson.Create(module.Id, "Lesson", "Description", 0);
    }
}
