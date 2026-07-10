using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Domain.Repositories;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class RequestVideoPlaybackUseCase
{
    private readonly IVideoRepository _videos;
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly CourseAccessService _courseAccessService;
    private readonly IVideoStorageService _videoStorageService;

    public RequestVideoPlaybackUseCase(
        IVideoRepository videos,
        ILessonRepository lessons,
        ICourseRepository courses,
        CourseAccessService courseAccessService,
        IVideoStorageService videoStorageService)
    {
        _videos = videos;
        _lessons = lessons;
        _courses = courses;
        _courseAccessService = courseAccessService;
        _videoStorageService = videoStorageService;
    }

    public async Task<VideoPlaybackOutput> ExecuteAsync(
        RequestVideoPlaybackInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (input.VideoId == Guid.Empty)
        {
            throw new ArgumentException("VideoId is required.", nameof(input));
        }

        var video = await _videos.FindByIdAsync(input.VideoId, cancellationToken);

        if (video is null)
        {
            throw new InvalidOperationException("Video not found.");
        }

        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video is not ready for playback.");
        }

        var lesson = await _lessons.FindByIdAsync(video.LessonId, cancellationToken);

        if (lesson is null)
        {
            throw new InvalidOperationException("Lesson not found.");
        }

        var course = await FindCourseByLessonAsync(lesson, cancellationToken);

        if (course is null)
        {
            throw new InvalidOperationException("Course not found for lesson.");
        }

        var access = await _courseAccessService.CanUserAccessCourseAsync(
            input.UserId,
            course.Id,
            cancellationToken);

        if (!access.CanAccess)
        {
            throw new UnauthorizedAccessException("User cannot access this video.");
        }

        var playbackUrl = await _videoStorageService.GeneratePlaybackUrlAsync(video, cancellationToken);

        return new VideoPlaybackOutput
        {
            VideoId = video.Id,
            LessonId = video.LessonId,
            Title = video.Title,
            PlaybackUrl = playbackUrl,
            DurationSeconds = video.DurationSeconds,
            Status = video.Status.ToString()
        };
    }

    private async Task<Course?> FindCourseByLessonAsync(
        Lesson lesson,
        CancellationToken cancellationToken)
    {
        var courses = await _courses.ListAsync(cancellationToken);

        foreach (var course in courses)
        {
            var details = await _courses.FindDetailsByIdAsync(course.Id, cancellationToken);

            if (details is not null && ContainsLesson(details, lesson))
            {
                return details;
            }
        }

        return null;
    }

    private static bool ContainsLesson(Course course, Lesson lesson)
    {
        return course.Modules.Any(module =>
            module.Id == lesson.ModuleId ||
            module.Lessons.Any(moduleLesson => moduleLesson.Id == lesson.Id));
    }
}
