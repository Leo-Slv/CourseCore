namespace CourseCore.Api.Modules.Questions.Application.DTOs;

public class AskLessonQuestionInput
{
    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public string QuestionText { get; init; } = string.Empty;
}
