using CourseCore.Api.Modules.Testimonials.Domain.Entities;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeTestimonialRepository : ITestimonialRepository
{
    public List<Testimonial> Testimonials { get; } = [];

    public Task<Testimonial?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Testimonials.FirstOrDefault(t => t.Id == id));
    }

    public Task<IReadOnlyCollection<Testimonial>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Testimonial>>(Testimonials.ToArray());
    }

    public Task<IReadOnlyCollection<Testimonial>> ListPublishedAsync(
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var published = Testimonials.Where(t => t.Published).AsEnumerable();

        if (limit is > 0)
        {
            published = published.Take(limit.Value);
        }

        return Task.FromResult<IReadOnlyCollection<Testimonial>>(published.ToArray());
    }

    public Task CreateAsync(Testimonial testimonial, CancellationToken cancellationToken = default)
    {
        Testimonials.Add(testimonial);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Testimonial testimonial, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
