namespace CourseCore.Api.Modules.Progress.Application.Options;

public sealed class ProgressOptions
{
    public const string SectionName = "Progress";

    public int LessonCompletionThresholdPercent { get; init; } = 90;

    public static void Validate(ProgressOptions options)
    {
        if (options.LessonCompletionThresholdPercent is < 1 or > 100)
        {
            throw new InvalidOperationException("Progress lesson completion threshold percent must be between 1 and 100.");
        }
    }
}
