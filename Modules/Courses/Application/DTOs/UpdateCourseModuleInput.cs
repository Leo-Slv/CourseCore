namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class UpdateCourseModuleInput
{
    public Guid ModuleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Published { get; init; }
}
