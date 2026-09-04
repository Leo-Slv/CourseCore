namespace CourseCore.Api.Modules.Progress.Presentation.Responses;

public class ModuleProgressResponse
{
    public Guid ModuleId { get; init; }

    public int LessonCount { get; init; }

    public int CompletedLessonCount { get; init; }

    public decimal ProgressPercent { get; init; }
}
