using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Media;

public class ImageUrlResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_WhenStoredValueIsNullOrWhitespace_ShouldReturnNull(string? storedValue)
    {
        var s3 = new FakeS3PresignedUrlProvider();
        var resolver = CreateResolver(s3);

        var result = await resolver.ResolveAsync(storedValue);

        Assert.Null(result);
        Assert.Equal(0, s3.DownloadCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenStoredValueIsAFullUrl_ShouldReturnItUnchangedWithoutSigning()
    {
        var s3 = new FakeS3PresignedUrlProvider();
        var resolver = CreateResolver(s3);
        const string legacyUrl = "https://example.com/foo.jpg";

        var result = await resolver.ResolveAsync(legacyUrl);

        Assert.Equal(legacyUrl, result);
        Assert.Equal(0, s3.DownloadCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenStoredValueIsABareStorageKey_ShouldReturnASignedDownloadUrl()
    {
        var s3 = new FakeS3PresignedUrlProvider { DownloadUrl = "https://fake-bucket.s3.amazonaws.com/signed" };
        var resolver = CreateResolver(s3, imageUrlExpirationMinutes: 60);
        const string storageKey = "course-thumbnails/abc/def.jpg";

        var result = await resolver.ResolveAsync(storageKey);

        Assert.Equal(s3.DownloadUrl, result);
        Assert.Equal(1, s3.DownloadCallCount);
        Assert.Equal(storageKey, s3.LastDownloadStorageKey);
        Assert.Equal(TimeSpan.FromMinutes(60), s3.LastDownloadExpiresIn);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUseTheConfiguredImageUrlExpirationMinutes()
    {
        var s3 = new FakeS3PresignedUrlProvider();
        var resolver = CreateResolver(s3, imageUrlExpirationMinutes: 120);

        await resolver.ResolveAsync("avatars/user/avatar.png");

        Assert.Equal(TimeSpan.FromMinutes(120), s3.LastDownloadExpiresIn);
    }

    private static ImageUrlResolver CreateResolver(
        FakeS3PresignedUrlProvider s3,
        int imageUrlExpirationMinutes = 60)
    {
        var options = Options.Create(new S3StorageOptions
        {
            BucketName = "fake-bucket",
            Region = "fake-region",
            ImageUrlExpirationMinutes = imageUrlExpirationMinutes
        });

        return new ImageUrlResolver(s3, options);
    }
}
