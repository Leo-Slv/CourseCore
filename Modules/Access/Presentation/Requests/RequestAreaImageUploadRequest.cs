namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class RequestAreaImageUploadRequest
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }
}
