using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class CreateVideoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPlaybackUrlIsProvided_ShouldIgnoreItAndKeepVideoProcessing()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Video",
            Description = "Description",
            StorageProvider = "Local",
            StorageKey = "videos/video.mp4",
            PlaybackUrl = "https://evil.example/phishing.mp4",
            DurationSeconds = 120,
            SizeBytes = 1024
        });

        var video = Assert.Single(fixture.Videos.Videos);
        Assert.Equal(VideoStatus.Processing, video.Status);
        Assert.Null(video.PlaybackUrl);
        Assert.Null(output.PlaybackUrl);
    }

    [Fact]
    public async Task ExecuteAsync_WhenYouTubeAndDurationIsKnown_ShouldMarkVideoAsReady()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Video",
            Description = "Description",
            StorageProvider = "YouTube",
            StorageKey = "dQw4w9WgXcQ",
            DurationSeconds = 120,
            SizeBytes = 0
        });

        Assert.Equal("Ready", output.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenYouTubeAndDurationIsZero_ShouldKeepVideoProcessing()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Video",
            Description = "Description",
            StorageProvider = "YouTube",
            StorageKey = "dQw4w9WgXcQ",
            DurationSeconds = 0,
            SizeBytes = 0
        });

        Assert.Equal("Processing", output.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocalAndDurationIsKnown_ShouldKeepVideoProcessing()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Video",
            Description = "Description",
            StorageProvider = "Local",
            StorageKey = "videos/video.mp4",
            DurationSeconds = 120,
            SizeBytes = 1024
        });

        Assert.Equal("Processing", output.Status);
    }

    [Theory]
    [InlineData("https://media.example/video.mp4")]
    [InlineData("../video.mp4")]
    [InlineData("videos/../video.mp4")]
    [InlineData("/videos/video.mp4")]
    [InlineData("videos\\video.mp4")]
    public async Task ExecuteAsync_WhenStorageKeyIsInvalid_ShouldRejectIt(string storageKey)
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAnyAsync<Exception>(() => fixture.UseCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Video",
            Description = "Description",
            StorageProvider = "Local",
            StorageKey = storageKey,
            DurationSeconds = 120,
            SizeBytes = 1024
        }));
    }

    private static CreateVideoFixture CreateFixture()
    {
        var videos = new FakeVideoRepository();
        var lessons = new FakeLessonRepository();
        var auditLogs = new FakeAuditLogService();
        var lesson = Lesson.Create(Guid.NewGuid(), "Lesson", "Description", displayOrder: 0);
        lessons.Lessons.Add(lesson);
        var useCase = new CreateVideoUseCase(videos, lessons, new FakeUnitOfWork(), auditLogs);

        return new CreateVideoFixture(useCase, videos, lesson);
    }

    private sealed record CreateVideoFixture(
        CreateVideoUseCase UseCase,
        FakeVideoRepository Videos,
        Lesson Lesson);
}
