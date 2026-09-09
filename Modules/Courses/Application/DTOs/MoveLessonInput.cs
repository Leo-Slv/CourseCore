namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class MoveLessonInput
{
    public Guid LessonId { get; init; }

    public Guid TargetModuleId { get; init; }
}
