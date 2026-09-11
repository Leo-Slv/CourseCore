using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Repositories;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class ListLessonMaterialsUseCase
{
    private readonly ILessonMaterialRepository _materials;

    public ListLessonMaterialsUseCase(ILessonMaterialRepository materials)
    {
        _materials = materials;
    }

    public async Task<IReadOnlyCollection<LessonMaterialOutput>> ExecuteAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }

        var materials = await _materials.ListByLessonIdAsync(lessonId, cancellationToken);

        return materials.Select(LessonMaterialOutput.FromMaterial).ToList();
    }
}
