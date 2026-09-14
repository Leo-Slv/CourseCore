using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Auth;

public class RequestAvatarUploadUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnUploadUrlWithAvatarsPrefix()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(
            fixture.UserId,
            "avatar.png",
            "image/png",
            1024);

        Assert.Equal("S3", output.StorageProvider);
        Assert.StartsWith($"avatars/{fixture.UserId:N}/", output.StorageKey);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(
            Guid.NewGuid(),
            "avatar.png",
            "image/png",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeIsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.UserId,
            "avatar.gif",
            "image/gif",
            1024));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSizeExceedsLimit_ShouldThrowValidationException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.UserId,
            "avatar.png",
            "image/png",
            long.MaxValue));
    }

    [Fact]
    public async Task ExecuteAsync_WhenS3IsNotAllowed_ShouldThrowValidationException()
    {
        var fixture = CreateFixture(allowedStorageProviders: ["Local"]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.UserId,
            "avatar.png",
            "image/png",
            1024));
    }

    private static RequestAvatarUploadFixture CreateFixture(IReadOnlyCollection<string>? allowedStorageProviders = null)
    {
        var users = new FakeUserRepository();
        var user = TestEntityFactory.User();
        users.Add(user);

        var s3 = new FakeS3PresignedUrlProvider();
        var playbackOptions = Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = "test-media-signing-secret-with-at-least-32-characters",
            AllowedStorageProviders = allowedStorageProviders ?? ["S3"]
        });
        var s3Options = Options.Create(new S3StorageOptions { BucketName = "fake-bucket", Region = "fake-region" });
        var imageUploadService = new ImageUploadService(s3, playbackOptions, s3Options);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RequestAvatarUploadUseCase(users, imageUploadService, auditLogs);

        return new RequestAvatarUploadFixture(useCase, user.Id);
    }

    private sealed record RequestAvatarUploadFixture(RequestAvatarUploadUseCase UseCase, Guid UserId);
}
