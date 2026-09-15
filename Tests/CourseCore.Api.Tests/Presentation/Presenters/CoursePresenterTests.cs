using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Presentation.Presenters;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Presentation.Presenters;

public class CoursePresenterTests
{
    [Fact]
    public async Task ToResponseAsync_WhenThumbnailUrlIsALegacyFullUrl_ShouldReturnItUnchanged()
    {
        const string legacyUrl = "https://example.com/foo.jpg";
        var s3 = new FakeS3PresignedUrlProvider();
        var resolver = CreateResolver(s3);
        var output = new CourseOutput
        {
            Id = Guid.NewGuid(),
            Title = "Course",
            Slug = "course",
            Description = "Description",
            ThumbnailUrl = legacyUrl,
            PricingModel = "Free",
            AreaIds = []
        };

        var response = await CoursePresenter.ToResponseAsync(output, resolver);

        Assert.Equal(legacyUrl, response.ThumbnailUrl);
        Assert.Equal(0, s3.DownloadCallCount);
    }

    [Fact]
    public async Task ToResponseAsync_WhenThumbnailUrlIsABareStorageKey_ShouldReturnASignedUrl()
    {
        var s3 = new FakeS3PresignedUrlProvider { DownloadUrl = "https://fake-bucket.s3.amazonaws.com/signed" };
        var resolver = CreateResolver(s3);
        var output = new CourseOutput
        {
            Id = Guid.NewGuid(),
            Title = "Course",
            Slug = "course",
            Description = "Description",
            ThumbnailUrl = "course-thumbnails/abc/def.jpg",
            PricingModel = "Free",
            AreaIds = []
        };

        var response = await CoursePresenter.ToResponseAsync(output, resolver);

        Assert.Equal(s3.DownloadUrl, response.ThumbnailUrl);
        Assert.Equal(1, s3.DownloadCallCount);
    }

    private static ImageUrlResolver CreateResolver(FakeS3PresignedUrlProvider s3)
    {
        var options = Options.Create(new S3StorageOptions
        {
            BucketName = "fake-bucket",
            Region = "fake-region"
        });

        return new ImageUrlResolver(s3, options);
    }
}
