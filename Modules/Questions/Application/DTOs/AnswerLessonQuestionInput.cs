namespace CourseCore.Api.Modules.Questions.Application.DTOs;

public class AnswerLessonQuestionInput
{
    public Guid QuestionId { get; init; }

    public Guid AnsweredByUserId { get; init; }

    public string AnswerText { get; init; } = string.Empty;
}
