using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.Validation;

public static class CourseInputValidator
{
    public static void Validate(CreateCourseInput input)
    {
        ValidateCourseFields(input.Title, input.Slug, input.Description, input.ThumbnailUrl, input.AreaIds);

        if (input.Modules is null || input.Modules.Count > CourseValidationLimits.MaxModules)
        {
            throw InvalidPayload();
        }

        foreach (var module in input.Modules)
        {
            if (module is null
                || !IsValidRequired(module.Title, CourseValidationLimits.ModuleTitleMaxLength)
                || !IsValidOptional(module.Description, CourseValidationLimits.ModuleDescriptionMaxLength)
                || module.Lessons is null
                || module.Lessons.Count > CourseValidationLimits.MaxLessonsPerModule)
            {
                throw InvalidPayload();
            }

            foreach (var lesson in module.Lessons)
            {
                if (lesson is null
                    || !IsValidRequired(lesson.Title, CourseValidationLimits.LessonTitleMaxLength)
                    || !IsValidOptional(lesson.Description, CourseValidationLimits.LessonDescriptionMaxLength))
                {
                    throw InvalidPayload();
                }
            }
        }
    }

    public static void Validate(UpdateCourseInput input)
    {
        ValidateCourseFields(input.Title, input.Slug, input.Description, input.ThumbnailUrl, input.AreaIds);
    }

    private static void ValidateCourseFields(
        string title,
        string slug,
        string description,
        string? thumbnailUrl,
        IReadOnlyCollection<Guid>? areaIds)
    {
        if (!IsValidRequired(title, CourseValidationLimits.TitleMaxLength)
            || !IsValidRequired(slug, CourseValidationLimits.SlugMaxLength)
            || !IsValidRequired(description, CourseValidationLimits.DescriptionMaxLength)
            || areaIds is null
            || areaIds.Count > CourseValidationLimits.MaxAreaIds
            || !IsValidHttpUrl(thumbnailUrl, CourseValidationLimits.ThumbnailUrlMaxLength))
        {
            throw InvalidPayload();
        }
    }

    private static bool IsValidRequired(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;

    private static bool IsValidOptional(string? value, int maxLength) =>
        value is null || value.Trim().Length <= maxLength;

    private static bool IsValidHttpUrl(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            && Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }

    private static ApplicationValidationException InvalidPayload() =>
        new("Course payload is invalid.");
}
