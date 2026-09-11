using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Application.Validation;
using CourseCore.Api.Modules.Questions.Domain.Entities;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Questions.Application.UseCases;

public class AskLessonQuestionUseCase
{
    private readonly IUserRepository _users;
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly ILessonQuestionRepository _questions;
    private readonly CourseAccessService _courseAccessService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public AskLessonQuestionUseCase(
        IUserRepository users,
        ILessonRepository lessons,
        ICourseRepository courses,
        ILessonQuestionRepository questions,
        CourseAccessService courseAccessService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _lessons = lessons;
        _courses = courses;
        _questions = questions;
        _courseAccessService = courseAccessService;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonQuestionOutput> ExecuteAsync(
        AskLessonQuestionInput input,
        CancellationToken cancellationToken = default)
    {
        QuestionInputValidator.Validate(input);

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

            var question = LessonQuestion.Create(input.LessonId, user.Id, user.Name, input.QuestionText);

            await _questions.CreateAsync(question, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.QuestionAsked,
                "LessonQuestion",
                question.Id,
                new Dictionary<string, string?>
                {
                    ["lessonId"] = question.LessonId.ToString(),
                    ["askedByUserId"] = question.AskedByUserId.ToString()
                },
                userId: user.Id,
                cancellationToken: cancellationToken);

            return LessonQuestionOutput.FromQuestion(question);
        }, cancellationToken);
    }
}
