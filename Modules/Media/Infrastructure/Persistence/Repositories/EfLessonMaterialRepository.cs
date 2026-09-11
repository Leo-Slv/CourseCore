using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Repositories;

public class EfLessonMaterialRepository : ILessonMaterialRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfLessonMaterialRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LessonMaterial?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : LessonMaterialMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<LessonMaterial>> ListByLessonIdAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.LessonMaterials
            .AsNoTracking()
            .Where(x => x.LessonId == lessonId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(LessonMaterialMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<LessonMaterial>>> ListByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default)
    {
        if (lessonIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<LessonMaterial>>();
        }

        var models = await _dbContext.LessonMaterials
            .AsNoTracking()
            .Where(x => lessonIds.Contains(x.LessonId))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models
            .Select(LessonMaterialMapper.ToDomain)
            .GroupBy(material => material.LessonId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<LessonMaterial>)group.ToList());
    }

    public async Task CreateAsync(LessonMaterial material, CancellationToken cancellationToken = default)
    {
        await _dbContext.LessonMaterials.AddAsync(LessonMaterialMapper.ToPersistence(material), cancellationToken);
    }

    public async Task UpdateAsync(LessonMaterial material, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonMaterials
            .FirstOrDefaultAsync(x => x.Id == material.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Lesson material not found.");
        }

        LessonMaterialMapper.ApplyChanges(material, model);
    }

    public async Task RemoveAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonMaterials
            .FirstOrDefaultAsync(x => x.Id == materialId, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Lesson material not found.");
        }

        _dbContext.LessonMaterials.Remove(model);
    }
}
