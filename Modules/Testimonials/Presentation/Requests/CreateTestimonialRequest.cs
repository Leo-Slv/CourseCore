namespace CourseCore.Api.Modules.Testimonials.Presentation.Requests;

public class CreateTestimonialRequest
{
    public string AuthorName { get; init; } = string.Empty;

    public string Quote { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public Guid? CourseId { get; init; }
}
