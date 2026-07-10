using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Domain.Repositories;

public interface IVideoRepository
{
    Task<Video?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Video?> FindByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);

    Task CreateAsync(Video video, CancellationToken cancellationToken = default);

    Task UpdateAsync(Video video, CancellationToken cancellationToken = default);
}
