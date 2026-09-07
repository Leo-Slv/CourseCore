using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Models;

public class TestimonialPersistenceModel
{
    public Guid Id { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Quote { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public Guid? CourseId { get; set; }

    public bool Published { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public CoursePersistenceModel? Course { get; set; }
}
