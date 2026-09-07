namespace CourseCore.Api.Modules.Testimonials.Application.DTOs;

public class UpdateTestimonialInput
{
    public Guid TestimonialId { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string Quote { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public Guid? CourseId { get; init; }
}
