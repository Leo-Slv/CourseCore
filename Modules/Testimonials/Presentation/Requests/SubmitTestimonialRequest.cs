namespace CourseCore.Api.Modules.Testimonials.Presentation.Requests;

public class SubmitTestimonialRequest
{
    public string Quote { get; init; } = string.Empty;

    public Guid? CourseId { get; init; }
}
