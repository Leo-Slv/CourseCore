namespace CourseCore.Api.Modules.Questions.Presentation.Responses;

public class LessonQuestionResponse
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
}
