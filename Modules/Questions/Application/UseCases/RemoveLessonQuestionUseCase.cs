using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Questions.Application.UseCases;

public class RemoveLessonQuestionUseCase
{
    private readonly ILessonQuestionRepository _questions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RemoveLessonQuestionUseCase(
        ILessonQuestionRepository questions,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _questions = questions;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        if (questionId == Guid.Empty)
        {
            throw new ArgumentException("QuestionId is required.", nameof(questionId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var question = await _questions.FindByIdAsync(questionId, cancellationToken);

            if (question is null)
            {
                throw new NotFoundException("Lesson question not found.");
            }

            await _questions.RemoveAsync(question.Id, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.QuestionRemoved,
                "LessonQuestion",
                question.Id,
                new Dictionary<string, string?>
                {
                    ["lessonId"] = question.LessonId.ToString()
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
