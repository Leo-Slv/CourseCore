using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Questions.Application.Validation;

public static class QuestionInputValidator
{
    public static void Validate(AskLessonQuestionInput input)
    {
        if (input.UserId == Guid.Empty
            || input.LessonId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.QuestionText)
            || input.QuestionText.Trim().Length > QuestionValidationLimits.QuestionTextMaxLength)
        {
            throw new ApplicationValidationException("Question payload is invalid.");
        }
    }

    public static void Validate(AnswerLessonQuestionInput input)
    {
        if (input.QuestionId == Guid.Empty
            || input.AnsweredByUserId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.AnswerText)
            || input.AnswerText.Trim().Length > QuestionValidationLimits.AnswerTextMaxLength)
        {
            throw new ApplicationValidationException("Answer payload is invalid.");
        }
    }
}
