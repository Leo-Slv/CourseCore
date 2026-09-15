using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Media;

public class ImageUploadServiceTests
{
    [Fact]
    public async Task RequestUploadAsync_WhenPayloadIsValid_ShouldReturnUploadUrlWithGeneratedStorageKey()
    {
        var service = CreateService(out _);
        var ownerId = Guid.NewGuid();

        var output = await service.RequestUploadAsync(
            "avatars",
            ownerId,
            "avatar.png",
            "image/png",
            1024);

        Assert.Equal("S3", output.StorageProvider);
        Assert.StartsWith($"avatars/{ownerId:N}/", output.StorageKey);
        Assert.EndsWith(".png", output.StorageKey);
        Assert.False(string.IsNullOrWhiteSpace(output.UploadUrl));
    }

    [Fact]
    public async Task RequestUploadAsync_WhenContentTypeIsNotAllowed_ShouldThrowValidationException()
    {
        var service = CreateService(out _);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.RequestUploadAsync(
            "avatars",
            Guid.NewGuid(),
            "avatar.gif",
            "image/gif",
            1024));
    }

    [Fact]
    public async Task RequestUploadAsync_WhenSizeExceedsLimit_ShouldThrowValidationException()
    {
        var service = CreateService(out _);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.RequestUploadAsync(
            "avatars",
            Guid.NewGuid(),
            "avatar.png",
            "image/png",
            MediaValidationLimits.MaxImageSizeBytes + 1));
    }

    [Fact]
    public async Task RequestUploadAsync_WhenFileNameIsMissing_ShouldThrowValidationException()
    {
        var service = CreateService(out _);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.RequestUploadAsync(
            "avatars",
            Guid.NewGuid(),
            string.Empty,
            "image/png",
            1024));
    }

    [Fact]
    public async Task RequestUploadAsync_WhenS3IsNotAnAllowedStorageProvider_ShouldThrowValidationException()
    {
        var service = CreateService(out _, allowedStorageProviders: ["Local"]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.RequestUploadAsync(
            "avatars",
            Guid.NewGuid(),
            "avatar.png",
            "image/png",
            1024));
    }

    private static ImageUploadService CreateService(
        out FakeS3PresignedUrlProvider s3,
        IReadOnlyCollection<string>? allowedStorageProviders = null)
    {
        s3 = new FakeS3PresignedUrlProvider();
        var playbackOptions = Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = "test-media-signing-secret-with-at-least-32-characters",
            AllowedStorageProviders = allowedStorageProviders ?? ["S3"]
        });
        var s3Options = Options.Create(new S3StorageOptions { BucketName = "fake-bucket", Region = "fake-region" });

        return new ImageUploadService(s3, playbackOptions, s3Options);
    }
}
