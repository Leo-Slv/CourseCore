using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

public class CoursePersistenceModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    public bool Published { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<CourseAreaPersistenceModel> CourseAreas { get; set; } = new List<CourseAreaPersistenceModel>();

    public ICollection<CourseModulePersistenceModel> Modules { get; set; } = new List<CourseModulePersistenceModel>();

    public ICollection<UserCourseProgressPersistenceModel> UserProgresses { get; set; } = new List<UserCourseProgressPersistenceModel>();
}
