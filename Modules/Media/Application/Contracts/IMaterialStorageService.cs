using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;

namespace CourseCore.Api.Modules.Media.Application.Contracts;

public interface IMaterialStorageService
{
    Task<string> GetUploadUrlAsync(
        MaterialStorageProvider provider,
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(LessonMaterial material, CancellationToken cancellationToken = default);
}
