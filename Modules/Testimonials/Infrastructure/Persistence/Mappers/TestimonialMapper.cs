using CourseCore.Api.Modules.Testimonials.Domain.Entities;
using CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Mappers;

public static class TestimonialMapper
{
    public static Testimonial ToDomain(TestimonialPersistenceModel model)
    {
        return Testimonial.Restore(
            model.Id,
            model.AuthorName,
            model.Quote,
            model.AvatarUrl,
            model.CourseId,
            model.Published,
            model.CreatedAt,
            model.UpdatedAt,
            model.SubmittedByUserId);
    }

    public static TestimonialPersistenceModel ToPersistence(Testimonial testimonial)
    {
        return new TestimonialPersistenceModel
        {
            Id = testimonial.Id,
            AuthorName = testimonial.AuthorName,
            Quote = testimonial.Quote,
            AvatarUrl = testimonial.AvatarUrl,
            CourseId = testimonial.CourseId,
            Published = testimonial.Published,
            SubmittedByUserId = testimonial.SubmittedByUserId,
            CreatedAt = testimonial.CreatedAt,
            UpdatedAt = testimonial.UpdatedAt
        };
    }

    public static void ApplyChanges(Testimonial testimonial, TestimonialPersistenceModel model)
    {
        model.AuthorName = testimonial.AuthorName;
        model.Quote = testimonial.Quote;
        model.AvatarUrl = testimonial.AvatarUrl;
        model.CourseId = testimonial.CourseId;
        model.Published = testimonial.Published;
        model.UpdatedAt = testimonial.UpdatedAt;
    }
}
