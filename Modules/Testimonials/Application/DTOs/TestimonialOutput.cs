using CourseCore.Api.Modules.Testimonials.Domain.Entities;

namespace CourseCore.Api.Modules.Testimonials.Application.DTOs;

public class TestimonialOutput
{
    public Guid Id { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string Quote { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public Guid? CourseId { get; init; }

    public bool Published { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static TestimonialOutput FromTestimonial(Testimonial testimonial)
    {
        return new TestimonialOutput
        {
            Id = testimonial.Id,
            AuthorName = testimonial.AuthorName,
            Quote = testimonial.Quote,
            AvatarUrl = testimonial.AvatarUrl,
            CourseId = testimonial.CourseId,
            Published = testimonial.Published,
            CreatedAt = testimonial.CreatedAt,
            UpdatedAt = testimonial.UpdatedAt
        };
    }
}
