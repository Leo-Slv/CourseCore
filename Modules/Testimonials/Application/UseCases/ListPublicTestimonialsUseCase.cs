using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;

namespace CourseCore.Api.Modules.Testimonials.Application.UseCases;

public class ListPublicTestimonialsUseCase
{
    private const int MaxResults = 3;

    private readonly ITestimonialRepository _testimonials;

    public ListPublicTestimonialsUseCase(ITestimonialRepository testimonials)
    {
        _testimonials = testimonials;
    }

    public async Task<IReadOnlyCollection<TestimonialOutput>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var testimonials = await _testimonials.ListPublishedAsync(MaxResults, cancellationToken);

        return testimonials.Select(TestimonialOutput.FromTestimonial).ToList();
    }
}
