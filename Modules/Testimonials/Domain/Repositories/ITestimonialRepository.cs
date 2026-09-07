using CourseCore.Api.Modules.Testimonials.Domain.Entities;

namespace CourseCore.Api.Modules.Testimonials.Domain.Repositories;

public interface ITestimonialRepository
{
    Task<Testimonial?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Testimonial>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Testimonial>> ListPublishedAsync(int? limit = null, CancellationToken cancellationToken = default);

    Task CreateAsync(Testimonial testimonial, CancellationToken cancellationToken = default);

    Task UpdateAsync(Testimonial testimonial, CancellationToken cancellationToken = default);
}
