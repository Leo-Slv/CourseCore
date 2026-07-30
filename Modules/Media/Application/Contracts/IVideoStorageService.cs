using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;

namespace CourseCore.Api.Modules.Media.Application.Contracts;

public interface IVideoStorageService
{
    Task<VideoPlaybackUrl> GeneratePlaybackUrlAsync(
        Video video,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<string> GetUploadUrlAsync(string storageKey, CancellationToken cancellationToken = default);
}
