using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Questions.Application.UseCases;

public class ListLessonQuestionsUseCase
{
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly ILessonQuestionRepository _questions;
    private readonly IUserRepository _users;
    private readonly CourseAccessService _courseAccessService;

    public ListLessonQuestionsUseCase(
        ILessonRepository lessons,
        ICourseRepository courses,
        ILessonQuestionRepository questions,
        IUserRepository users,
        CourseAccessService courseAccessService)
    {
        _lessons = lessons;
        _courses = courses;
        _questions = questions;
        _users = users;
        _courseAccessService = courseAccessService;
    }

    public async Task<IReadOnlyCollection<LessonQuestionOutput>> ExecuteAsync(
        Guid userId,
        Guid lessonId,
        bool bypassAccessCheck = false,
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

        if (!bypassAccessCheck)
        {
            var access = await _courseAccessService.CanUserAccessCourseAsync(userId, course.Id, cancellationToken);

            if (!access.CanAccess)
            {
                throw new ForbiddenException("User cannot access this course.");
            }
        }

        var questions = await _questions.ListByLessonIdAsync(lessonId, cancellationToken);

        var authorIds = questions
            .Select(question => question.AskedByUserId)
            .Concat(questions
                .Where(question => question.AnsweredByUserId.HasValue)
                .Select(question => question.AnsweredByUserId!.Value))
            .Distinct()
            .ToList();

        var authors = await _users.FindByIdsAsync(authorIds, cancellationToken);
        var avatarsByUserId = authors.ToDictionary(author => author.Id, author => author.AvatarUrl);

        return questions
            .Select(question => LessonQuestionOutput.FromQuestion(
                question,
                askedByAvatarUrl: avatarsByUserId.GetValueOrDefault(question.AskedByUserId),
                answeredByAvatarUrl: question.AnsweredByUserId.HasValue
                    ? avatarsByUserId.GetValueOrDefault(question.AnsweredByUserId.Value)
                    : null))
            .ToList();
    }
}
