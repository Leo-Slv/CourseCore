using CourseCore.Api.Modules.Testimonials.Domain.Entities;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;
using CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Repositories;

public class EfTestimonialRepository : ITestimonialRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfTestimonialRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Testimonial?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Testimonials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : TestimonialMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Testimonial>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Testimonials
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(TestimonialMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<Testimonial>> ListPublishedAsync(
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Testimonials
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.CreatedAt)
            .AsQueryable();

        if (limit is > 0)
        {
            query = query.Take(limit.Value);
        }

        var models = await query.ToListAsync(cancellationToken);

        return models.Select(TestimonialMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(Testimonial testimonial, CancellationToken cancellationToken = default)
    {
        await _dbContext.Testimonials.AddAsync(TestimonialMapper.ToPersistence(testimonial), cancellationToken);
    }

    public async Task UpdateAsync(Testimonial testimonial, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Testimonials
            .FirstOrDefaultAsync(x => x.Id == testimonial.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Testimonial not found.");
        }

        TestimonialMapper.ApplyChanges(testimonial, model);
    }
}
