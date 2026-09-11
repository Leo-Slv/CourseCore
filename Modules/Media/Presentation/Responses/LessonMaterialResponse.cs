namespace CourseCore.Api.Modules.Media.Presentation.Responses;

public class LessonMaterialResponse
{
    public Guid Id { get; init; }

    public Guid LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public int DisplayOrder { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
