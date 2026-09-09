using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class ListVideosUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnVideosAcrossDifferentLessons()
    {
        var videos = new FakeVideoRepository();
        videos.Videos.Add(Video.Create(
            Guid.NewGuid(), "Video 1", "Description", VideoStorageProvider.Local, "videos/one.mp4", 100, 0));
        videos.Videos.Add(Video.Create(
            Guid.NewGuid(), "Video 2", "Description", VideoStorageProvider.Local, "videos/two.mp4", 100, 0));
        var useCase = new ListVideosUseCase(videos);

        var output = await useCase.ExecuteAsync(new ListVideosInput());

        Assert.Equal(2, output.Items.Count);
        Assert.Equal(2, output.TotalItems);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeriveYouTubeFieldsOnlyForYouTubeProvider()
    {
        var videos = new FakeVideoRepository();
        videos.Videos.Add(Video.Create(
            Guid.NewGuid(), "YouTube Video", "Description", VideoStorageProvider.YouTube, "dQw4w9WgXcQ", 100, 0));
        videos.Videos.Add(Video.Create(
            Guid.NewGuid(), "Local Video", "Description", VideoStorageProvider.Local, "videos/local.mp4", 100, 0));
        var useCase = new ListVideosUseCase(videos);

        var output = await useCase.ExecuteAsync(new ListVideosInput());

        var youTubeItem = Assert.Single(output.Items, item => item.Title == "YouTube Video");
        Assert.Equal("dQw4w9WgXcQ", youTubeItem.YouTubeVideoId);
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", youTubeItem.YouTubeUrl);

        var localItem = Assert.Single(output.Items, item => item.Title == "Local Video");
        Assert.Null(localItem.YouTubeVideoId);
        Assert.Null(localItem.YouTubeUrl);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPageResults()
    {
        var videos = new FakeVideoRepository();
        for (var i = 0; i < 5; i++)
        {
            videos.Videos.Add(Video.Create(
                Guid.NewGuid(), $"Video {i}", "Description", VideoStorageProvider.Local, $"videos/{i}.mp4", 100, 0));
        }
        var useCase = new ListVideosUseCase(videos);

        var output = await useCase.ExecuteAsync(new ListVideosInput { Page = 1, PageSize = 2 });

        Assert.Equal(2, output.Items.Count);
        Assert.Equal(5, output.TotalItems);
        Assert.Equal(3, output.TotalPages);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageIsInvalid_ShouldThrow()
    {
        var useCase = new ListVideosUseCase(new FakeVideoRepository());

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new ListVideosInput { Page = 0 }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageSizeIsInvalid_ShouldThrow()
    {
        var useCase = new ListVideosUseCase(new FakeVideoRepository());

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new ListVideosInput { PageSize = 0 }));
    }
}
