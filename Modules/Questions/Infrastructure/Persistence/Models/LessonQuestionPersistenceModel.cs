using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Models;

public class LessonQuestionPersistenceModel
{
    public Guid Id { get; set; }

    public Guid LessonId { get; set; }

    public Guid AskedByUserId { get; set; }

    public string AskedByName { get; set; } = string.Empty;

    public string QuestionText { get; set; } = string.Empty;

    public string? AnswerText { get; set; }

    public Guid? AnsweredByUserId { get; set; }

    public string? AnsweredByName { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public LessonPersistenceModel? Lesson { get; set; }
}
