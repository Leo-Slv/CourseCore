namespace CourseCore.Api.Modules.Certificates.Application.DTOs;

public class CertificateOutput
{
    public Guid Id { get; init; }

    public Guid CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public string CourseSlug { get; init; } = string.Empty;

    public DateTime IssuedAt { get; init; }
}
