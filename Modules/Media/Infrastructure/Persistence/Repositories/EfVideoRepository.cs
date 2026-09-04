using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Repositories;

public class EfVideoRepository : IVideoRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfVideoRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Video?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Videos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : VideoMapper.ToDomain(model);
    }

    public async Task<Video?> FindByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Videos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LessonId == lessonId, cancellationToken);

        return model is null ? null : VideoMapper.ToDomain(model);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ListDurationSecondsByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        if (lessonIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await _dbContext.Videos
            .AsNoTracking()
            .Where(video => lessonIds.Contains(video.LessonId))
            .Select(video => new { video.LessonId, video.DurationSeconds })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.LessonId, row => row.DurationSeconds);
    }

    public async Task<IReadOnlyDictionary<Guid, Video>> ListByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        if (lessonIds.Count == 0)
        {
            return new Dictionary<Guid, Video>();
        }

        var models = await _dbContext.Videos
            .AsNoTracking()
            .Where(video => lessonIds.Contains(video.LessonId))
            .ToListAsync(cancellationToken);

        return models.ToDictionary(model => model.LessonId, VideoMapper.ToDomain);
    }

    public async Task CreateAsync(Video video, CancellationToken cancellationToken = default)
    {
        await _dbContext.Videos.AddAsync(VideoMapper.ToPersistence(video), cancellationToken);
    }

    public async Task UpdateAsync(Video video, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Videos
            .FirstOrDefaultAsync(x => x.Id == video.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Video not found.");
        }

        VideoMapper.ApplyChanges(video, model);
    }
}
