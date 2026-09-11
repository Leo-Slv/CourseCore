using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Questions.Application.UseCases;

public class ListLessonQuestionsUseCase
{
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly ILessonQuestionRepository _questions;
    private readonly CourseAccessService _courseAccessService;

    public ListLessonQuestionsUseCase(
        ILessonRepository lessons,
        ICourseRepository courses,
        ILessonQuestionRepository questions,
        CourseAccessService courseAccessService)
    {
        _lessons = lessons;
        _courses = courses;
        _questions = questions;
        _courseAccessService = courseAccessService;
    }

    public async Task<IReadOnlyCollection<LessonQuestionOutput>> ExecuteAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }

        var lesson = await _lessons.FindByIdAsync(lessonId, cancellationToken);

        if (lesson is null)
        {
            throw new NotFoundException("Lesson not found.");
        }

        var course = await _courses.FindByLessonIdAsync(lesson.Id, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException("Course not found for lesson.");
        }

        var access = await _courseAccessService.CanUserAccessCourseAsync(userId, course.Id, cancellationToken);

        if (!access.CanAccess)
        {
            throw new ForbiddenException("User cannot access this course.");
        }

        var questions = await _questions.ListByLessonIdAsync(lessonId, cancellationToken);

        return questions.Select(LessonQuestionOutput.FromQuestion).ToList();
    }
}
