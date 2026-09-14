namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class RequestUploadInput
{
    public Guid LessonId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public string StorageProvider { get; init; } = string.Empty;
}
