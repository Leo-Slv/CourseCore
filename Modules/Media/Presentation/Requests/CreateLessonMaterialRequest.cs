namespace CourseCore.Api.Modules.Media.Presentation.Requests;

public class CreateLessonMaterialRequest
{
    public string Title { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public int? DisplayOrder { get; init; }
}
