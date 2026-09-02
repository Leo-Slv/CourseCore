namespace CourseCore.Api.Modules.Courses.Application.Validation;

public static class CourseValidationLimits
{
    public const int TitleMaxLength = 200;
    public const int SlugMaxLength = 220;
    public const int DescriptionMaxLength = 2000;
    public const int ThumbnailUrlMaxLength = 1000;
    public const int PricingModelMaxLength = 20;
    public const int ModuleTitleMaxLength = 200;
    public const int ModuleDescriptionMaxLength = 1000;
    public const int LessonTitleMaxLength = 200;
    public const int LessonDescriptionMaxLength = 1000;
    public const int MaxAreaIds = 50;
    public const int MaxModules = 50;
    public const int MaxLessonsPerModule = 100;
}
