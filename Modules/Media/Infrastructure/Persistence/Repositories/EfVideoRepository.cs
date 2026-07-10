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
