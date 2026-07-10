using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Modules.Progress.Application.UseCases;

public class RegisterLessonProgressUseCase
{
    private readonly IUserRepository _users;
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly IProgressRepository _progress;
    private readonly CourseAccessService _courseAccessService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterLessonProgressUseCase(
        IUserRepository users,
        ILessonRepository lessons,
        ICourseRepository courses,
        IProgressRepository progress,
        CourseAccessService courseAccessService,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _lessons = lessons;
        _courses = courses;
        _progress = progress;
        _courseAccessService = courseAccessService;
        _unitOfWork = unitOfWork;
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
                throw new InvalidOperationException("User not found.");
            }

            if (!user.Active)
            {
                throw new UnauthorizedAccessException("User is inactive.");
            }

            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

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
                throw new UnauthorizedAccessException("User cannot access this course.");
            }

            var lessonProgress = await _progress.FindLessonProgressAsync(
                input.UserId,
                input.LessonId,
                cancellationToken) ?? UserLessonProgress.Create(input.UserId, input.LessonId);

            lessonProgress.RegisterWatch(input.WatchedSeconds);

            if (input.MarkAsCompleted)
            {
                lessonProgress.MarkAsCompleted();
            }

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
