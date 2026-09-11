using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Application.Contracts;

public interface IMaterialStorageService
{
    Task<string> GetUploadUrlAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(LessonMaterial material, CancellationToken cancellationToken = default);
}
