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
}
