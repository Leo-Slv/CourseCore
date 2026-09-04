namespace CourseCore.Api.Modules.Courses.Domain.Repositories;

public sealed record CourseContentSummary(
    Guid CourseId,
    int ModuleCount,
    int LessonCount,
    IReadOnlyCollection<Guid> LessonIds);
