using CourseCore.Api.Modules.Certificates.Domain.Entities;
using CourseCore.Api.Modules.Certificates.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Certificates.Infrastructure.Persistence.Mappers;

public static class CertificateMapper
{
    public static Certificate ToDomain(CertificatePersistenceModel model)
    {
        return Certificate.Restore(
            model.Id,
            model.UserId,
            model.CourseId,
            model.IssuedAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static CertificatePersistenceModel ToPersistence(Certificate certificate)
    {
        return new CertificatePersistenceModel
        {
            Id = certificate.Id,
            UserId = certificate.UserId,
            CourseId = certificate.CourseId,
            IssuedAt = certificate.IssuedAt,
            CreatedAt = certificate.CreatedAt,
            UpdatedAt = certificate.UpdatedAt
        };
    }

    public static void ApplyChanges(Certificate certificate, CertificatePersistenceModel model)
    {
        model.IssuedAt = certificate.IssuedAt;
        model.UpdatedAt = certificate.UpdatedAt;
    }
}
