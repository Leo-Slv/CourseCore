using CourseCore.Api.Modules.Questions.Domain.Entities;

namespace CourseCore.Api.Modules.Questions.Application.DTOs;

public class LessonQuestionOutput
{
    public Guid Id { get; init; }

    public Guid LessonId { get; init; }

    public Guid AskedByUserId { get; init; }

    public string AskedByName { get; init; } = string.Empty;

    public string QuestionText { get; init; } = string.Empty;

    public string? AnswerText { get; init; }

    public Guid? AnsweredByUserId { get; init; }

    public string? AnsweredByName { get; init; }

    public DateTime? AnsweredAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static LessonQuestionOutput FromQuestion(LessonQuestion question)
    {
        return new LessonQuestionOutput
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
}
