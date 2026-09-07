using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class GetLessonVideoUseCase
{
    private readonly IVideoRepository _videos;

    public GetLessonVideoUseCase(IVideoRepository videos)
    {
        _videos = videos;
    }

    public async Task<VideoOutput> ExecuteAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }

        var video = await _videos.FindByLessonIdAsync(lessonId, cancellationToken);

        if (video is null)
        {
            throw new NotFoundException("No video is registered for this lesson.");
        }

        return VideoOutput.FromVideo(video);
    }
}
