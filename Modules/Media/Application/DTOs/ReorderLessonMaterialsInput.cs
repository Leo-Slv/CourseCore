namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class ReorderLessonMaterialsInput
{
    public Guid LessonId { get; init; }

    public IReadOnlyCollection<ReorderLessonMaterialItem> Items { get; init; } = [];
}

public class ReorderLessonMaterialItem
{
    public Guid MaterialId { get; init; }

    public int DisplayOrder { get; init; }
}
