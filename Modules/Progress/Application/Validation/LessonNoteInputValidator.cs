using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Progress.Application.Validation;

public static class LessonNoteInputValidator
{
    public static void Validate(UpsertLessonNoteInput input)
    {
        if (input.UserId == Guid.Empty
            || input.LessonId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.Content)
            || input.Content.Trim().Length > LessonNoteValidationLimits.ContentMaxLength)
        {
            throw new ApplicationValidationException("Note payload is invalid.");
        }
    }
}
