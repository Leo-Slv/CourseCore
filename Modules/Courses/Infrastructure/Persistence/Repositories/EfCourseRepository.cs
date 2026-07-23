using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Repositories;

public class EfCourseRepository : ICourseRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfCourseRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await CourseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : CourseMapper.ToDomain(model);
    }

    public async Task<Course?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await CourseDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : CourseMapper.ToDomain(model);
    }

    public async Task<Course?> FindByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var model = await CourseDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                course => course.Modules.Any(module => module.Lessons.Any(lesson => lesson.Id == lessonId)),
                cancellationToken);

        return model is null ? null : CourseMapper.ToDomain(model);
    }

    public async Task<Course?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        var model = await CourseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug.Value, cancellationToken);

        return model is null ? null : CourseMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Course>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await CourseQuery()
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return models.Select(CourseMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<Course>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        var models = await CourseQuery()
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return models.Select(CourseMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<Course>> ListByAreaIdsAsync(
        IReadOnlyCollection<Guid> areaIds,
        CancellationToken cancellationToken = default)
    {
        if (areaIds.Count == 0)
        {
            return [];
        }

        var models = await CourseQuery()
            .AsNoTracking()
            .Where(x => x.CourseAreas.Any(courseArea => areaIds.Contains(courseArea.AreaId)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return models.Select(CourseMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(Course course, CancellationToken cancellationToken = default)
    {
        await _dbContext.Courses.AddAsync(CourseMapper.ToPersistence(course), cancellationToken);
    }

    public async Task UpdateAsync(Course course, CancellationToken cancellationToken = default)
    {
        var model = await CourseDetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == course.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Course not found.");
        }

        CourseMapper.ApplyChanges(course, model);
    }

    public Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Courses.AnyAsync(x => x.Slug == slug.Value, cancellationToken);
    }

    private IQueryable<CoursePersistenceModel> CourseQuery()
    {
        return _dbContext.Courses
            .Include(x => x.CourseAreas)
            .Include(x => x.Modules);
    }

    private IQueryable<CoursePersistenceModel> CourseDetailsQuery()
    {
        return _dbContext.Courses
            .Include(x => x.CourseAreas)
            .Include(x => x.Modules)
                .ThenInclude(x => x.Lessons);
    }
}
