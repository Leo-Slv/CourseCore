using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Repositories;

public class EfProgressRepository : IProgressRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfProgressRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserLessonProgress?> FindLessonProgressAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserLessonProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, cancellationToken);

        return model is null ? null : ProgressMapper.ToDomain(model);
    }

    public async Task<UserCourseProgress?> FindCourseProgressAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserCourseProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, cancellationToken);

        return model is null ? null : ProgressMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<UserLessonProgress>> ListLessonProgressByCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.UserLessonProgress
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Lesson != null && x.Lesson.Module != null && x.Lesson.Module.CourseId == courseId)
            .OrderBy(x => x.LastWatchedAt)
            .ToListAsync(cancellationToken);

        return models.Select(ProgressMapper.ToDomain).ToList();
    }

    public async Task SaveLessonProgressAsync(
        UserLessonProgress progress,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserLessonProgress
            .FirstOrDefaultAsync(x => x.UserId == progress.UserId && x.LessonId == progress.LessonId, cancellationToken);

        if (model is null)
        {
            await _dbContext.UserLessonProgress.AddAsync(ProgressMapper.ToPersistence(progress), cancellationToken);
            return;
        }

        ProgressMapper.ApplyChanges(progress, model);
    }

    public async Task SaveCourseProgressAsync(
        UserCourseProgress progress,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserCourseProgress
            .FirstOrDefaultAsync(x => x.UserId == progress.UserId && x.CourseId == progress.CourseId, cancellationToken);

        if (model is null)
        {
            await _dbContext.UserCourseProgress.AddAsync(ProgressMapper.ToPersistence(progress), cancellationToken);
            return;
        }

        ProgressMapper.ApplyChanges(progress, model);
    }
}
