using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Courses;

public class RequestCourseModuleImageUploadUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenModuleExists_ShouldReturnUploadUrlWithModuleCoversPrefix()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            fixture.ModuleId,
            "cover.png",
            "image/png",
            1024);

        Assert.Equal("S3", output.StorageProvider);
        Assert.StartsWith($"module-covers/{fixture.ModuleId:N}/", output.StorageKey);
    }

    [Fact]
    public async Task ExecuteAsync_WhenModuleDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            Guid.NewGuid(),
            "cover.png",
            "image/png",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenModuleBelongsToAnotherCourse_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(
            Guid.NewGuid(),
            fixture.ModuleId,
            "cover.png",
            "image/png",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeIsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            fixture.ModuleId,
            "cover.gif",
            "image/gif",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSizeExceedsLimit_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            fixture.ModuleId,
            "cover.png",
            "image/png",
            long.MaxValue));
    }

    [Fact]
    public async Task ExecuteAsync_WhenS3IsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture(allowedStorageProviders: ["Local"]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.CourseId,
            fixture.ModuleId,
            "cover.png",
            "image/png",
            1024));
    }

    private static RequestCourseModuleImageUploadFixture CreateFixture(IReadOnlyCollection<string>? allowedStorageProviders = null)
    {
        var courseModules = new FakeCourseModuleRepository();
        var courseId = Guid.NewGuid();
        var module = CourseModule.Create(courseId, "Module", "Description", displayOrder: 0);
        courseModules.Modules.Add(module);

        var s3 = new FakeS3PresignedUrlProvider();
        var playbackOptions = Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = "test-media-signing-secret-with-at-least-32-characters",
            AllowedStorageProviders = allowedStorageProviders ?? ["S3"]
        });
        var s3Options = Options.Create(new S3StorageOptions { BucketName = "fake-bucket", Region = "fake-region" });
        var imageUploadService = new ImageUploadService(s3, playbackOptions, s3Options);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RequestCourseModuleImageUploadUseCase(courseModules, imageUploadService, auditLogs);

        return new RequestCourseModuleImageUploadFixture(useCase, courseId, module.Id);
    }

    private sealed record RequestCourseModuleImageUploadFixture(
        RequestCourseModuleImageUploadUseCase UseCase,
        Guid CourseId,
        Guid ModuleId);
}
