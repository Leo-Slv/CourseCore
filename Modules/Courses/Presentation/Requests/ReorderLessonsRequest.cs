namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class ReorderLessonsRequest
{
    public IReadOnlyList<Guid> LessonIds { get; init; } = Array.Empty<Guid>();
}
