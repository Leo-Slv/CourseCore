using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Application.Options;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Progress.Application.UseCases;

public class RegisterLessonProgressUseCase
{
    private readonly IUserRepository _users;
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly IVideoRepository _videos;
    private readonly IProgressRepository _progress;
    private readonly CourseAccessService _courseAccessService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProgressOptions _options;

    public RegisterLessonProgressUseCase(
        IUserRepository users,
        ILessonRepository lessons,
        ICourseRepository courses,
        IVideoRepository videos,
        IProgressRepository progress,
        CourseAccessService courseAccessService,
        IUnitOfWork unitOfWork,
        IOptions<ProgressOptions> options)
    {
        _users = users;
        _lessons = lessons;
        _courses = courses;
        _videos = videos;
        _progress = progress;
        _courseAccessService = courseAccessService;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        ProgressOptions.Validate(_options);
    }

    public Task<LessonProgressOutput> ExecuteAsync(
        RegisterLessonProgressInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(input);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (!user.Active)
            {
                throw new ForbiddenException("User is inactive.");
            }

            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

            if (lesson is null)
            {
                throw new NotFoundException("Lesson not found.");
            }

            var course = await _courses.FindByLessonIdAsync(lesson.Id, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found for lesson.");
            }

            var access = await _courseAccessService.CanUserAccessCourseAsync(
                input.UserId,
                course.Id,
                cancellationToken);

            if (!access.CanAccess)
            {
                throw new ForbiddenException("User cannot access this course.");
            }

            var lessonProgress = await _progress.FindLessonProgressAsync(
                input.UserId,
                input.LessonId,
                cancellationToken) ?? UserLessonProgress.Create(input.UserId, input.LessonId);

            var video = await _videos.FindByLessonIdAsync(input.LessonId, cancellationToken);
            var maxWatchedSeconds = video is not null && video.DurationSeconds >= 0
                ? video.DurationSeconds
                : (int?)null;

            lessonProgress.RegisterWatch(input.WatchedSeconds, maxWatchedSeconds);
            lessonProgress.RecalculateCompletion(IsLessonCompletedByServer(video, lessonProgress.WatchedSeconds));

            await _progress.SaveLessonProgressAsync(lessonProgress, cancellationToken);

            var courseProgress = await _progress.FindCourseProgressAsync(
                input.UserId,
                course.Id,
                cancellationToken) ?? UserCourseProgress.Create(input.UserId, course.Id);

            var lessonProgresses = await _progress.ListLessonProgressByCourseAsync(
                input.UserId,
                course.Id,
                cancellationToken);

            var progressPercent = CalculateProgressPercent(course, lessonProgresses, lessonProgress);

            courseProgress.Recalculate(progressPercent);

            if (progressPercent == 100)
            {
                courseProgress.MarkAsCompleted();
            }

            await _progress.SaveCourseProgressAsync(courseProgress, cancellationToken);

            return LessonProgressOutput.FromProgress(lessonProgress);
        }, cancellationToken);
    }

    private bool IsLessonCompletedByServer(Video? video, int watchedSeconds)
    {
        if (video is null || video.Status != VideoStatus.Ready || video.DurationSeconds <= 0)
        {
            return false;
        }

        var completionThresholdSeconds = (int)Math.Ceiling(
            video.DurationSeconds * (_options.LessonCompletionThresholdPercent / 100m));

        return watchedSeconds >= completionThresholdSeconds;
    }

    private static decimal CalculateProgressPercent(
        Course course,
        IReadOnlyCollection<UserLessonProgress> existingProgresses,
        UserLessonProgress currentProgress)
    {
        var lessonIds = course.Modules
            .SelectMany(module => module.Lessons)
            .Select(lesson => lesson.Id)
            .ToHashSet();

        if (lessonIds.Count == 0)
        {
            return 0;
        }

        var progresses = existingProgresses
            .Where(progress => lessonIds.Contains(progress.LessonId))
            .ToDictionary(progress => progress.LessonId);

        progresses[currentProgress.LessonId] = currentProgress;

        var completedLessons = progresses.Values.Count(progress => progress.Completed);
        var progressPercent = Math.Round((decimal)completedLessons / lessonIds.Count * 100m, 2);

        return Math.Clamp(progressPercent, 0, 100);
    }

    private static void ValidateInput(RegisterLessonProgressInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (input.LessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(input));
        }

        if (input.WatchedSeconds < 0)
        {
            throw new ArgumentException("WatchedSeconds cannot be negative.", nameof(input));
        }
    }
}
