namespace CourseCore.Api.Modules.Auth.Presentation.Requests;

public class RequestAvatarUploadRequest
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }
}
