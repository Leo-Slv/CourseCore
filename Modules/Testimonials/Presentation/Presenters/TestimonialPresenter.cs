using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Presentation.Requests;
using CourseCore.Api.Modules.Testimonials.Presentation.Responses;

namespace CourseCore.Api.Modules.Testimonials.Presentation.Presenters;

public static class TestimonialPresenter
{
    public static CreateTestimonialInput ToInput(CreateTestimonialRequest request)
    {
        return new CreateTestimonialInput
        {
            AuthorName = request.AuthorName,
            Quote = request.Quote,
            AvatarUrl = request.AvatarUrl,
            CourseId = request.CourseId
        };
    }

    public static UpdateTestimonialInput ToInput(Guid testimonialId, UpdateTestimonialRequest request)
    {
        return new UpdateTestimonialInput
        {
            TestimonialId = testimonialId,
            AuthorName = request.AuthorName,
            Quote = request.Quote,
            AvatarUrl = request.AvatarUrl,
            CourseId = request.CourseId
        };
    }

    public static SubmitTestimonialInput ToInput(Guid userId, SubmitTestimonialRequest request)
    {
        return new SubmitTestimonialInput
        {
            UserId = userId,
            Quote = request.Quote,
            CourseId = request.CourseId
        };
    }

    public static TestimonialResponse ToResponse(TestimonialOutput output)
    {
        return new TestimonialResponse
        {
            Id = output.Id,
            AuthorName = output.AuthorName,
            Quote = output.Quote,
            AvatarUrl = output.AvatarUrl,
            CourseId = output.CourseId,
            Published = output.Published,
            SubmittedByUserId = output.SubmittedByUserId,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }
}
