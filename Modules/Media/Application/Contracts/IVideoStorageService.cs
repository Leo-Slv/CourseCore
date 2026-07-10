using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Application.Contracts;

public interface IVideoStorageService
{
    Task<string> GeneratePlaybackUrlAsync(Video video, CancellationToken cancellationToken = default);

    Task<string> GetUploadUrlAsync(string storageKey, CancellationToken cancellationToken = default);
}
