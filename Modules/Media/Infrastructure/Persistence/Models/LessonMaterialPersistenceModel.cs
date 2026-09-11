using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;

public class LessonMaterialPersistenceModel
{
    public Guid Id { get; set; }

    public Guid LessonId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public LessonPersistenceModel? Lesson { get; set; }
}
