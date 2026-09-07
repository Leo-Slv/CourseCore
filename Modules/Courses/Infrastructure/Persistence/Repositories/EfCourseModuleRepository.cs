using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Repositories;

public class EfCourseModuleRepository : ICourseModuleRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfCourseModuleRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CourseModule?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.CourseModules
            .AsNoTracking()
            .Include(x => x.Lessons)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : CourseModuleMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<CourseModule>> ListByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.CourseModules
            .AsNoTracking()
            .Include(x => x.Lessons)
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return models.Select(CourseModuleMapper.ToDomain).ToList();
    }

    public async Task AddAsync(CourseModule module, CancellationToken cancellationToken = default)
    {
        await _dbContext.CourseModules.AddAsync(CourseModuleMapper.ToPersistence(module), cancellationToken);
    }

    public async Task UpdateAsync(CourseModule module, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.CourseModules
            .FirstOrDefaultAsync(x => x.Id == module.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Course module not found.");
        }

        CourseModuleMapper.ApplyChanges(module, model);
    }

    public async Task RemoveAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.CourseModules
            .FirstOrDefaultAsync(x => x.Id == moduleId, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Course module not found.");
        }

        _dbContext.CourseModules.Remove(model);
    }

    public async Task ReorderAsync(
        Guid courseId,
        IReadOnlyList<Guid> orderedModuleIds,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.CourseModules
            .Where(x => x.CourseId == courseId)
            .ToListAsync(cancellationToken);

        var modelsById = models.ToDictionary(x => x.Id);

        if (models.Count != orderedModuleIds.Count
            || orderedModuleIds.Distinct().Count() != orderedModuleIds.Count
            || orderedModuleIds.Any(id => !modelsById.ContainsKey(id)))
        {
            throw new ArgumentException("Ordered module ids must match the course's existing modules exactly.", nameof(orderedModuleIds));
        }

        for (var i = 0; i < models.Count; i++)
        {
            models[i].DisplayOrder = -(i + 1);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < orderedModuleIds.Count; i++)
        {
            modelsById[orderedModuleIds[i]].DisplayOrder = i;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
