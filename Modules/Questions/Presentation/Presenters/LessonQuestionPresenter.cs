using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Presentation.Requests;
using CourseCore.Api.Modules.Questions.Presentation.Responses;

namespace CourseCore.Api.Modules.Questions.Presentation.Presenters;

public static class LessonQuestionPresenter
{
    public static AskLessonQuestionInput ToInput(Guid userId, Guid lessonId, AskLessonQuestionRequest request)
    {
        return new AskLessonQuestionInput
        {
            UserId = userId,
            LessonId = lessonId,
            QuestionText = request.QuestionText
        };
    }

    public static AnswerLessonQuestionInput ToInput(
        Guid questionId,
        Guid answeredByUserId,
        AnswerLessonQuestionRequest request)
    {
        return new AnswerLessonQuestionInput
        {
            QuestionId = questionId,
            AnsweredByUserId = answeredByUserId,
            AnswerText = request.AnswerText
        };
    }

    public static LessonQuestionResponse ToResponse(LessonQuestionOutput output)
    {
        return new LessonQuestionResponse
        {
            Id = output.Id,
            LessonId = output.LessonId,
            AskedByUserId = output.AskedByUserId,
            AskedByName = output.AskedByName,
            QuestionText = output.QuestionText,
            AnswerText = output.AnswerText,
            AnsweredByUserId = output.AnsweredByUserId,
            AnsweredByName = output.AnsweredByName,
            AnsweredAt = output.AnsweredAt,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }
}
