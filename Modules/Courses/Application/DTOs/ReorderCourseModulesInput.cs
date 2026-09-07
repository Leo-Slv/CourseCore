namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class ReorderCourseModulesInput
{
    public Guid CourseId { get; init; }

    public IReadOnlyList<Guid> OrderedModuleIds { get; init; } = Array.Empty<Guid>();
}
