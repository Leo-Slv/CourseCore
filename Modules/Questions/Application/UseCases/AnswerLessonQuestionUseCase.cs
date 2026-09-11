using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Application.Validation;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Questions.Application.UseCases;

public class AnswerLessonQuestionUseCase
{
    private readonly ILessonQuestionRepository _questions;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public AnswerLessonQuestionUseCase(
        ILessonQuestionRepository questions,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _questions = questions;
        _users = users;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonQuestionOutput> ExecuteAsync(
        AnswerLessonQuestionInput input,
        CancellationToken cancellationToken = default)
    {
        QuestionInputValidator.Validate(input);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var question = await _questions.FindByIdAsync(input.QuestionId, cancellationToken);

            if (question is null)
            {
                throw new NotFoundException("Lesson question not found.");
            }

            var answeredByUser = await _users.FindByIdAsync(input.AnsweredByUserId, cancellationToken);

            if (answeredByUser is null)
            {
                throw new NotFoundException("User not found.");
            }

            question.Answer(answeredByUser.Id, answeredByUser.Name, input.AnswerText);

            await _questions.UpdateAsync(question, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.QuestionAnswered,
                "LessonQuestion",
                question.Id,
                new Dictionary<string, string?>
                {
                    ["lessonId"] = question.LessonId.ToString(),
                    ["answeredByUserId"] = question.AnsweredByUserId.ToString()
                },
                userId: answeredByUser.Id,
                cancellationToken: cancellationToken);

            return LessonQuestionOutput.FromQuestion(question);
        }, cancellationToken);
    }
}
