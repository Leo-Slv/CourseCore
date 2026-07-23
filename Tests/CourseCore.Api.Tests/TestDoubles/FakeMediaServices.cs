using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Repositories;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeVideoRepository : IVideoRepository
{
    public List<Video> Videos { get; } = [];

    public Task<Video?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Videos.FirstOrDefault(video => video.Id == id));
    }

    public Task<Video?> FindByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Videos.FirstOrDefault(video => video.LessonId == lessonId));
    }

    public Task CreateAsync(Video video, CancellationToken cancellationToken = default)
    {
        Videos.Add(video);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Video video, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class FakeVideoStorageService : IVideoStorageService
{
    public string PlaybackUrl { get; set; } = "https://media.coursecore.local/playback";

    public Task<string> GeneratePlaybackUrlAsync(Video video, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PlaybackUrl);
    }

    public Task<string> GetUploadUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://media.coursecore.local/upload/{storageKey}");
    }
}
