using CourseCore.Api.Modules.Certificates.Domain.Entities;
using CourseCore.Api.Modules.Certificates.Domain.Repositories;
using CourseCore.Api.Modules.Certificates.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Certificates.Infrastructure.Persistence.Repositories;

public class EfCertificateRepository : ICertificateRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfCertificateRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Certificate?> FindByUserAndCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, cancellationToken);

        return model is null ? null : CertificateMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Certificate>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Certificates
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);

        return models.Select(CertificateMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, Certificate>> ListByUserIdAndCourseIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
        {
            return new Dictionary<Guid, Certificate>();
        }

        var models = await _dbContext.Certificates
            .AsNoTracking()
            .Where(x => x.UserId == userId && courseIds.Contains(x.CourseId))
            .ToListAsync(cancellationToken);

        return models.ToDictionary(model => model.CourseId, CertificateMapper.ToDomain);
    }

    public async Task CreateAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Certificates.AddAsync(CertificateMapper.ToPersistence(certificate), cancellationToken);
    }
}
