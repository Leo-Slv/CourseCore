namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class ReorderCourseModulesRequest
{
    public IReadOnlyList<Guid> ModuleIds { get; init; } = Array.Empty<Guid>();
}
