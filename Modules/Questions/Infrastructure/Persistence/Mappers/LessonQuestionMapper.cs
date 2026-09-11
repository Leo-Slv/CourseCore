using CourseCore.Api.Modules.Questions.Domain.Entities;
using CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Mappers;

public static class LessonQuestionMapper
{
    public static LessonQuestion ToDomain(LessonQuestionPersistenceModel model)
    {
        return LessonQuestion.Restore(
            model.Id,
            model.LessonId,
            model.AskedByUserId,
            model.AskedByName,
            model.QuestionText,
            model.AnswerText,
            model.AnsweredByUserId,
            model.AnsweredByName,
            model.AnsweredAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static LessonQuestionPersistenceModel ToPersistence(LessonQuestion question)
    {
        return new LessonQuestionPersistenceModel
        {
            Id = question.Id,
            LessonId = question.LessonId,
            AskedByUserId = question.AskedByUserId,
            AskedByName = question.AskedByName,
            QuestionText = question.QuestionText,
            AnswerText = question.AnswerText,
            AnsweredByUserId = question.AnsweredByUserId,
            AnsweredByName = question.AnsweredByName,
            AnsweredAt = question.AnsweredAt,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    public static void ApplyChanges(LessonQuestion question, LessonQuestionPersistenceModel model)
    {
        model.LessonId = question.LessonId;
        model.AskedByUserId = question.AskedByUserId;
        model.AskedByName = question.AskedByName;
        model.QuestionText = question.QuestionText;
        model.AnswerText = question.AnswerText;
        model.AnsweredByUserId = question.AnsweredByUserId;
        model.AnsweredByName = question.AnsweredByName;
        model.AnsweredAt = question.AnsweredAt;
        model.UpdatedAt = question.UpdatedAt;
    }
}
