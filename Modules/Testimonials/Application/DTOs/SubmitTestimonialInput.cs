namespace CourseCore.Api.Modules.Testimonials.Application.DTOs;

public class SubmitTestimonialInput
{
    public Guid UserId { get; init; }

    public string Quote { get; init; } = string.Empty;

    public Guid? CourseId { get; init; }
}
