using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;

namespace CourseCore.Api.Modules.Testimonials.Application.UseCases;

public class ListTestimonialsUseCase
{
    private readonly ITestimonialRepository _testimonials;

    public ListTestimonialsUseCase(ITestimonialRepository testimonials)
    {
        _testimonials = testimonials;
    }

    public async Task<IReadOnlyCollection<TestimonialOutput>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var testimonials = await _testimonials.ListAsync(cancellationToken);

        return testimonials.Select(TestimonialOutput.FromTestimonial).ToList();
    }
}
