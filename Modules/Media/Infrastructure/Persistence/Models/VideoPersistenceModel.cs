using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;

public class VideoPersistenceModel
{
    public Guid Id { get; set; }

    public Guid LessonId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string? PlaybackUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int DurationSeconds { get; set; }

    public long SizeBytes { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public LessonPersistenceModel? Lesson { get; set; }
}
