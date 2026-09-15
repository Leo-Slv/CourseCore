using System.Web;
using Amazon.S3;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Infrastructure.Media;

public class S3PresignedUrlProviderTests
{
    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_WhenBucketIsNotConfigured_ShouldThrow()
    {
        var provider = new S3PresignedUrlProvider(
            new AmazonS3Client(Amazon.RegionEndpoint.USEast1),
            Options.Create(new S3StorageOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GeneratePresignedUploadUrlAsync("videos/lesson/file.mp4", "video/mp4"));
    }

    [Fact]
    public async Task GeneratePresignedDownloadUrlAsync_WhenBucketIsNotConfigured_ShouldThrow()
    {
        var provider = new S3PresignedUrlProvider(
            new AmazonS3Client(Amazon.RegionEndpoint.USEast1),
            Options.Create(new S3StorageOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GeneratePresignedDownloadUrlAsync("videos/lesson/file.mp4"));
    }

    [Fact]
    public async Task GeneratePresignedDownloadUrlAsync_WhenExpiresInIsProvided_ShouldOverrideConfiguredDefault()
    {
        var provider = new S3PresignedUrlProvider(
            new AmazonS3Client(Amazon.RegionEndpoint.USEast1),
            Options.Create(new S3StorageOptions
            {
                BucketName = "fake-bucket",
                Region = "us-east-1",
                DownloadUrlExpirationMinutes = 10
            }));

        var defaultUrl = await provider.GeneratePresignedDownloadUrlAsync("images/foo.png");
        var overriddenUrl = await provider.GeneratePresignedDownloadUrlAsync(
            "images/foo.png",
            TimeSpan.FromMinutes(60));

        var defaultExpires = ExtractExpiresSeconds(defaultUrl);
        var overriddenExpires = ExtractExpiresSeconds(overriddenUrl);

        Assert.Equal(10 * 60, defaultExpires);
        Assert.Equal(60 * 60, overriddenExpires);
    }

    private static int ExtractExpiresSeconds(string presignedUrl)
    {
        var query = HttpUtility.ParseQueryString(new Uri(presignedUrl).Query);
        var value = query["X-Amz-Expires"] ?? throw new InvalidOperationException("X-Amz-Expires missing from presigned URL.");

        return int.Parse(value);
    }
}
