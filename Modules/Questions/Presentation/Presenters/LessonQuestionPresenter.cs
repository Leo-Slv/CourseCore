using CourseCore.Api.Modules.Media.Application.Services;
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

    public static async Task<LessonQuestionResponse> ToResponseAsync(
        LessonQuestionOutput output,
        ImageUrlResolver imageUrlResolver,
        CancellationToken cancellationToken = default)
    {
        return new LessonQuestionResponse
        {
            Id = output.Id,
            LessonId = output.LessonId,
            AskedByUserId = output.AskedByUserId,
            AskedByName = output.AskedByName,
            AskedByAvatarUrl = await imageUrlResolver.ResolveAsync(output.AskedByAvatarUrl, cancellationToken),
            QuestionText = output.QuestionText,
            AnswerText = output.AnswerText,
            AnsweredByUserId = output.AnsweredByUserId,
            AnsweredByName = output.AnsweredByName,
            AnsweredByAvatarUrl = await imageUrlResolver.ResolveAsync(output.AnsweredByAvatarUrl, cancellationToken),
            AnsweredAt = output.AnsweredAt,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }
}
