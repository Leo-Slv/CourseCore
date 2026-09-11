using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Domain.Repositories;

public interface ILessonMaterialRepository
{
    Task<LessonMaterial?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LessonMaterial>> ListByLessonIdAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<LessonMaterial>>> ListByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds,
        CancellationToken cancellationToken = default);

    Task CreateAsync(LessonMaterial material, CancellationToken cancellationToken = default);

    Task UpdateAsync(LessonMaterial material, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid materialId, CancellationToken cancellationToken = default);
}
