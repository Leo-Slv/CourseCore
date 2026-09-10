using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;

namespace CourseCore.Api.Modules.Testimonials.Application.UseCases;

public class ListPublicTestimonialsUseCase
{
    private readonly ITestimonialRepository _testimonials;

    public ListPublicTestimonialsUseCase(ITestimonialRepository testimonials)
    {
        _testimonials = testimonials;
    }

    public async Task<IReadOnlyCollection<TestimonialOutput>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var testimonials = await _testimonials.ListPublishedAsync(cancellationToken: cancellationToken);

        return testimonials.Select(TestimonialOutput.FromTestimonial).ToList();
    }
}
