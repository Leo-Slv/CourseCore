using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
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

    public Task<IReadOnlyDictionary<Guid, int>> ListDurationSecondsByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        var lessonIdSet = lessonIds.ToHashSet();
        IReadOnlyDictionary<Guid, int> result = Videos
            .Where(video => lessonIdSet.Contains(video.LessonId))
            .ToDictionary(video => video.LessonId, video => video.DurationSeconds);

        return Task.FromResult(result);
    }

    public Task<IReadOnlyDictionary<Guid, Video>> ListByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        var lessonIdSet = lessonIds.ToHashSet();
        IReadOnlyDictionary<Guid, Video> result = Videos
            .Where(video => lessonIdSet.Contains(video.LessonId))
            .ToDictionary(video => video.LessonId, video => video);

        return Task.FromResult(result);
    }

    public Task<(IReadOnlyCollection<Video> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ordered = Videos.OrderByDescending(video => video.CreatedAt).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult<(IReadOnlyCollection<Video> Items, int TotalCount)>((items, ordered.Count));
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

    public Task RemoveAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        Videos.RemoveAll(video => video.Id == videoId);

        return Task.CompletedTask;
    }
}

public sealed class FakeVideoStorageService : IVideoStorageService
{
    public string PlaybackUrl { get; set; } = "https://media.coursecore.local/playback";

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);

    public Task<VideoPlaybackUrl> GeneratePlaybackUrlAsync(
        Video video,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new VideoPlaybackUrl(PlaybackUrl, ExpiresAt));
    }

    public Task<string> GetUploadUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://media.coursecore.local/upload/{storageKey}");
    }
}

public sealed class FakeLessonMaterialRepository : ILessonMaterialRepository
{
    public List<LessonMaterial> Materials { get; } = [];

    public Task<LessonMaterial?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Materials.FirstOrDefault(material => material.Id == id));
    }

    public Task<IReadOnlyCollection<LessonMaterial>> ListByLessonIdAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<LessonMaterial> result = Materials
            .Where(material => material.LessonId == lessonId)
            .OrderBy(material => material.DisplayOrder)
            .ThenBy(material => material.CreatedAt)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<LessonMaterial>>> ListByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        var lessonIdSet = lessonIds.ToHashSet();
        IReadOnlyDictionary<Guid, IReadOnlyCollection<LessonMaterial>> result = Materials
            .Where(material => lessonIdSet.Contains(material.LessonId))
            .GroupBy(material => material.LessonId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<LessonMaterial>)group.ToList());

        return Task.FromResult(result);
    }

    public Task CreateAsync(LessonMaterial material, CancellationToken cancellationToken = default)
    {
        Materials.Add(material);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(LessonMaterial material, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        Materials.RemoveAll(material => material.Id == materialId);

        return Task.CompletedTask;
    }
}

public sealed class FakeMaterialStorageService : IMaterialStorageService
{
    public string DownloadUrl { get; set; } = "https://media.coursecore.local/download";

    public Task<string> GetUploadUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://media.coursecore.local/upload/{storageKey}");
    }

    public Task<string> GetDownloadUrlAsync(LessonMaterial material, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DownloadUrl);
    }
}
