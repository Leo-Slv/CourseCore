using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Courses;

public class RequestCourseThumbnailUploadUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCourseExists_ShouldReturnUploadUrlWithCourseThumbnailsPrefix()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            "thumbnail.png",
            "image/png",
            1024);

        Assert.Equal("S3", output.StorageProvider);
        Assert.StartsWith($"course-thumbnails/{fixture.CourseId:N}/", output.StorageKey);
        Assert.False(string.IsNullOrWhiteSpace(output.PublicUrl));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(
            Guid.NewGuid(),
            "thumbnail.png",
            "image/png",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeIsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            "thumbnail.gif",
            "image/gif",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSizeExceedsLimit_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            "thumbnail.png",
            "image/png",
            long.MaxValue));
    }

    [Fact]
    public async Task ExecuteAsync_WhenS3IsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture(allowedStorageProviders: ["Local"]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            "thumbnail.png",
            "image/png",
            1024));
    }

    private static RequestCourseThumbnailUploadFixture CreateFixture(IReadOnlyCollection<string>? allowedStorageProviders = null)
    {
        var courses = new FakeCourseRepository();
        var course = Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", displayOrder: 0);
        courses.Courses.Add(course);

        var s3 = new FakeS3PresignedUrlProvider();
        var playbackOptions = Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = "test-media-signing-secret-with-at-least-32-characters",
            AllowedStorageProviders = allowedStorageProviders ?? ["S3"]
        });
        var s3Options = Options.Create(new S3StorageOptions { BucketName = "fake-bucket", Region = "fake-region" });
        var imageUploadService = new ImageUploadService(s3, playbackOptions, s3Options);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RequestCourseThumbnailUploadUseCase(courses, imageUploadService, auditLogs);

        return new RequestCourseThumbnailUploadFixture(useCase, course.Id);
    }

    private sealed record RequestCourseThumbnailUploadFixture(RequestCourseThumbnailUploadUseCase UseCase, Guid CourseId);
}
