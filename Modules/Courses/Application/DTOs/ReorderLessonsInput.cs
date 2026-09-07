namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class ReorderLessonsInput
{
    public Guid ModuleId { get; init; }

    public IReadOnlyList<Guid> OrderedLessonIds { get; init; } = Array.Empty<Guid>();
}
