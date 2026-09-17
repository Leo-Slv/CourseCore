using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Tests.Application.Courses;

public class CourseInputValidatorTests
{
    [Fact]
    public void Validate_UpdateCourseInput_WhenThumbnailUrlIsABareStorageKey_ShouldNotThrow()
    {
        var input = new UpdateCourseInput
        {
            CourseId = Guid.NewGuid(),
            Title = "Course",
            Slug = "course",
            Description = "Description",
            ThumbnailUrl = "course-thumbnails/abc/def.jpg",
            PricingModel = "Free",
            AreaIds = [Guid.NewGuid()]
        };

        CourseInputValidator.Validate(input);
    }

    [Fact]
    public void ValidateModuleFields_WhenImageUrlIsABareStorageKey_ShouldNotThrow()
    {
        CourseInputValidator.ValidateModuleFields(
            "Module",
            "Description",
            "module-covers/9f600ce28869449eb2af4ef8154a7c5c/3b9959146a0245c8a6cda78af229915e.jpg");
    }

    [Fact]
    public void ValidateModuleFields_WhenImageUrlExceedsMaxLength_ShouldThrowApplicationValidationException()
    {
        var tooLong = new string('a', CourseValidationLimits.ModuleImageUrlMaxLength + 1);

        Assert.Throws<ApplicationValidationException>(
            () => CourseInputValidator.ValidateModuleFields("Module", "Description", tooLong));
    }
}
