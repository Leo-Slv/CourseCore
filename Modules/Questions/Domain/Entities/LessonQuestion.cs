using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Questions.Domain.Entities;

public class LessonQuestion : EntityBase
{
    private const int MaxQuestionTextLength = 2000;
    private const int MaxAnswerTextLength = 2000;

    private LessonQuestion(
        Guid lessonId,
        Guid askedByUserId,
        string askedByName,
        string questionText,
        string? answerText,
        Guid? answeredByUserId,
        string? answeredByName,
        DateTime? answeredAt)
    {
        LessonId = ValidateId(lessonId, nameof(LessonId));
        AskedByUserId = ValidateId(askedByUserId, nameof(AskedByUserId));
        AskedByName = ValidateRequired(askedByName, nameof(AskedByName));
        QuestionText = ValidateQuestionText(questionText);
        AnswerText = answerText;
        AnsweredByUserId = answeredByUserId;
        AnsweredByName = answeredByName;
        AnsweredAt = answeredAt;
    }

    public Guid LessonId { get; private set; }

    public Guid AskedByUserId { get; private set; }

    public string AskedByName { get; private set; }

    public string QuestionText { get; private set; }

    public string? AnswerText { get; private set; }

    public Guid? AnsweredByUserId { get; private set; }

    public string? AnsweredByName { get; private set; }

    public DateTime? AnsweredAt { get; private set; }

    public static LessonQuestion Create(Guid lessonId, Guid askedByUserId, string askedByName, string questionText)
    {
        return new LessonQuestion(
            lessonId,
            askedByUserId,
            askedByName,
            questionText,
            answerText: null,
            answeredByUserId: null,
            answeredByName: null,
            answeredAt: null);
    }

    public static LessonQuestion Restore(
        Guid id,
        Guid lessonId,
        Guid askedByUserId,
        string askedByName,
        string questionText,
        string? answerText,
        Guid? answeredByUserId,
        string? answeredByName,
        DateTime? answeredAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new LessonQuestion(
            lessonId,
            askedByUserId,
            askedByName,
            questionText,
            answerText,
            answeredByUserId,
            answeredByName,
            answeredAt)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Answer(Guid answeredByUserId, string answeredByName, string answerText)
    {
        AnsweredByUserId = ValidateId(answeredByUserId, nameof(AnsweredByUserId));
        AnsweredByName = ValidateRequired(answeredByName, nameof(AnsweredByName));
        AnswerText = ValidateAnswerText(answerText);
        AnsweredAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    private static Guid ValidateId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return id;
    }

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string ValidateQuestionText(string questionText)
    {
        var normalized = ValidateRequired(questionText, nameof(QuestionText));

        if (normalized.Length > MaxQuestionTextLength)
        {
            throw new DomainException("QuestionText is too long.");
        }

        return normalized;
    }

    private static string ValidateAnswerText(string answerText)
    {
        var normalized = ValidateRequired(answerText, nameof(AnswerText));

        if (normalized.Length > MaxAnswerTextLength)
        {
            throw new DomainException("AnswerText is too long.");
        }

        return normalized;
    }
}
