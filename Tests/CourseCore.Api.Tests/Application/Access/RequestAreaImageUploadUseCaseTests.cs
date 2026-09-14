using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Access;

public class RequestAreaImageUploadUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAreaExists_ShouldReturnUploadUrlWithAreaCoversPrefix()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(
            fixture.AreaId,
            "cover.png",
            "image/png",
            1024);

        Assert.Equal("S3", output.StorageProvider);
        Assert.StartsWith($"area-covers/{fixture.AreaId:N}/", output.StorageKey);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAreaDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(
            Guid.NewGuid(),
            "cover.png",
            "image/png",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeIsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.AreaId,
            "cover.gif",
            "image/gif",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSizeExceedsLimit_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.AreaId,
            "cover.png",
            "image/png",
            long.MaxValue));
    }

    [Fact]
    public async Task ExecuteAsync_WhenS3IsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture(allowedStorageProviders: ["Local"]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.AreaId,
            "cover.png",
            "image/png",
            1024));
    }

    private static RequestAreaImageUploadFixture CreateFixture(IReadOnlyCollection<string>? allowedStorageProviders = null)
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);

        var s3 = new FakeS3PresignedUrlProvider();
        var playbackOptions = Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = "test-media-signing-secret-with-at-least-32-characters",
            AllowedStorageProviders = allowedStorageProviders ?? ["S3"]
        });
        var s3Options = Options.Create(new S3StorageOptions { BucketName = "fake-bucket", Region = "fake-region" });
        var imageUploadService = new ImageUploadService(s3, playbackOptions, s3Options);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RequestAreaImageUploadUseCase(areas, imageUploadService, auditLogs);

        return new RequestAreaImageUploadFixture(useCase, area.Id);
    }

    private sealed record RequestAreaImageUploadFixture(RequestAreaImageUploadUseCase UseCase, Guid AreaId);
}
