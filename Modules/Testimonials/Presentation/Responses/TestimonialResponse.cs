namespace CourseCore.Api.Modules.Testimonials.Presentation.Responses;

public class TestimonialResponse
{
    public Guid Id { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string Quote { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public Guid? CourseId { get; init; }

    public bool Published { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
