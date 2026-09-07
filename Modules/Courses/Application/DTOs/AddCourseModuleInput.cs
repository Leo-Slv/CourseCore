namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class AddCourseModuleInput
{
    public Guid CourseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
