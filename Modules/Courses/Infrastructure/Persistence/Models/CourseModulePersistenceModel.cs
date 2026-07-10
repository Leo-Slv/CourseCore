namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

public class CourseModulePersistenceModel
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool Published { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public CoursePersistenceModel? Course { get; set; }

    public ICollection<LessonPersistenceModel> Lessons { get; set; } = new List<LessonPersistenceModel>();
}
