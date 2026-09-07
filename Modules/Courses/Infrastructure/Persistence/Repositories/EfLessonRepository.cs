using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Repositories;

public class EfLessonRepository : ILessonRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfLessonRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Lesson?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : LessonMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Lesson>> ListByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Lessons
            .AsNoTracking()
            .Where(x => x.ModuleId == moduleId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return models.Select(LessonMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<Lesson>> ListByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Lessons
            .AsNoTracking()
            .Where(x => x.Module != null && x.Module.CourseId == courseId)
            .OrderBy(x => x.ModuleId)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return models.Select(LessonMapper.ToDomain).ToList();
    }

    public async Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        await _dbContext.Lessons.AddAsync(LessonMapper.ToPersistence(lesson), cancellationToken);
    }

    public async Task UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Lessons
            .FirstOrDefaultAsync(x => x.Id == lesson.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Lesson not found.");
        }

        LessonMapper.ApplyChanges(lesson, model);
    }

    public async Task RemoveAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Lessons
            .FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Lesson not found.");
        }

        _dbContext.Lessons.Remove(model);
    }

    public async Task ReorderAsync(
        Guid moduleId,
        IReadOnlyList<Guid> orderedLessonIds,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Lessons
            .Where(x => x.ModuleId == moduleId)
            .ToListAsync(cancellationToken);

        var modelsById = models.ToDictionary(x => x.Id);

        if (models.Count != orderedLessonIds.Count
            || orderedLessonIds.Distinct().Count() != orderedLessonIds.Count
            || orderedLessonIds.Any(id => !modelsById.ContainsKey(id)))
        {
            throw new ArgumentException("Ordered lesson ids must match the module's existing lessons exactly.", nameof(orderedLessonIds));
        }

        for (var i = 0; i < models.Count; i++)
        {
            models[i].DisplayOrder = -(i + 1);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < orderedLessonIds.Count; i++)
        {
            modelsById[orderedLessonIds[i]].DisplayOrder = i;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
