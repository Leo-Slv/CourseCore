using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Media;

public class RequestLessonMaterialUploadUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPayloadIsValid_ShouldReturnUploadUrlWithGeneratedStorageKey()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new RequestUploadInput
        {
            LessonId = fixture.Lesson.Id,
            FileName = "apostila-modulo-01.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            StorageProvider = "Local"
        });

        Assert.Equal("Local", output.StorageProvider);
        Assert.StartsWith($"materials/{fixture.Lesson.Id:N}/", output.StorageKey);
        Assert.EndsWith(".pdf", output.StorageKey);
        Assert.False(string.IsNullOrWhiteSpace(output.UploadUrl));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new RequestUploadInput
        {
            LessonId = Guid.NewGuid(),
            FileName = "apostila.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            StorageProvider = "Local"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeIsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new RequestUploadInput
        {
            LessonId = fixture.Lesson.Id,
            FileName = "apostila.exe",
            ContentType = "application/x-msdownload",
            SizeBytes = 1024,
            StorageProvider = "Local"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSizeExceedsLimit_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new RequestUploadInput
        {
            LessonId = fixture.Lesson.Id,
            FileName = "apostila.pdf",
            ContentType = "application/pdf",
            SizeBytes = long.MaxValue,
            StorageProvider = "Local"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageProviderIsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new RequestUploadInput
        {
            LessonId = fixture.Lesson.Id,
            FileName = "apostila.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            StorageProvider = "S3"
        }));
    }

    private static RequestLessonMaterialUploadFixture CreateFixture()
    {
        var lessons = new FakeLessonRepository();
        var lesson = Lesson.Create(Guid.NewGuid(), "Lesson", "Description", displayOrder: 0);
        lessons.Lessons.Add(lesson);

        var materialStorageService = new FakeMaterialStorageService();
        var playbackOptions = Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = "test-media-signing-secret-with-at-least-32-characters",
            AllowedStorageProviders = ["Local"]
        });
        var s3Options = Options.Create(new S3StorageOptions());
        var auditLogs = new FakeAuditLogService();
        var useCase = new RequestLessonMaterialUploadUseCase(lessons, materialStorageService, playbackOptions, s3Options, auditLogs);

        return new RequestLessonMaterialUploadFixture(useCase, lesson);
    }

    private sealed record RequestLessonMaterialUploadFixture(RequestLessonMaterialUploadUseCase UseCase, Lesson Lesson);
}
