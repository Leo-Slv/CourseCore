namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class RequestCourseModuleImageUploadRequest
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }
}
