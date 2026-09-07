using CourseCore.Api.Modules.Certificates.Application.DTOs;
using CourseCore.Api.Modules.Certificates.Presentation.Responses;

namespace CourseCore.Api.Modules.Certificates.Presentation.Presenters;

public static class CertificatePresenter
{
    public static CertificateResponse ToResponse(CertificateOutput output)
    {
        return new CertificateResponse
        {
            Id = output.Id,
            CourseId = output.CourseId,
            CourseTitle = output.CourseTitle,
            CourseSlug = output.CourseSlug,
            IssuedAt = output.IssuedAt
        };
    }
}
