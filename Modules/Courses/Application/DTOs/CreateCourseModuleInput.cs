namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class CreateCourseModuleInput
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public IReadOnlyCollection<CreateLessonInput> Lessons { get; init; } = Array.Empty<CreateLessonInput>();
}
