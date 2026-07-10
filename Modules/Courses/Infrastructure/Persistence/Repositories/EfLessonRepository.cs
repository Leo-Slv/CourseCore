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
}
